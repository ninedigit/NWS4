using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NineDigit.NWS4;

//public delegate Task BodyChunkAsyncWriterDelegate(byte[] chunk, CancellationToken cancellationToken = default);

public class AuthorizationHeaderChunkedSigner : AuthorizationHeaderSigner
{
    // SHA256 substitute marker used in place of x-amz-content-sha256 when employing 
    // chunked uploads
    protected internal const string StreamingBodySha256 = "STREAMING-AWS4-HMAC-SHA256-PAYLOAD";
    internal const string ContentEncodingNwsChunked = "nws-chunked";
    private const string ClRf = "\r\n";
    private const string ChunkStringToSignPrefix = "AWS4-HMAC-SHA256-PAYLOAD";
    private const string ChunkSignatureHeader = ";chunk-signature=";
    private const int SignatureLength = 64;

    public static readonly long MinBlockSize = CalculateChunkTotalLength(1);
    static readonly byte[] FinalChunk = Array.Empty<byte>();

    public AuthorizationHeaderChunkedSigner()
        : this(CreateDefaultOptions(), DefaultDateTimeProvider.Instance)
    {
    }

    public AuthorizationHeaderChunkedSigner(IDateTimeProvider dateTimeProvider)
        : this(CreateDefaultOptions(), dateTimeProvider, NullLogger<AuthorizationHeaderChunkedSigner>.Instance)
    {
    }
        
    public AuthorizationHeaderChunkedSigner(
        IDateTimeProvider dateTimeProvider,
        ILogger<AuthorizationHeaderChunkedSigner> logger)
        : this(CreateDefaultOptions(), dateTimeProvider, logger)
    {
    }
        
    public AuthorizationHeaderChunkedSigner(
        AuthorizationHeaderChunkedSignerOptions options)
        : this(options, DefaultDateTimeProvider.Instance)
    {
    }
        
    public AuthorizationHeaderChunkedSigner(
        AuthorizationHeaderChunkedSignerOptions options,
        ILogger<AuthorizationHeaderChunkedSigner> logger)
        : this(options, DefaultDateTimeProvider.Instance, logger)
    {
    }

    public AuthorizationHeaderChunkedSigner(
        AuthorizationHeaderChunkedSignerOptions options,
        IDateTimeProvider dateTimeProvider)
        : this(options, dateTimeProvider, NullLogger<AuthorizationHeaderChunkedSigner>.Instance)
    {
    }
        
    public AuthorizationHeaderChunkedSigner(
        AuthorizationHeaderChunkedSignerOptions options,
        IDateTimeProvider dateTimeProvider,
        ILogger<AuthorizationHeaderChunkedSigner> logger)
        : base(options, dateTimeProvider, logger)
    {
    }

    public async Task<RawChunks> SignRequestAsync(
        IHttpRequest request,
        string accessKey,
        string privateKey,
        long blockSize,
        CancellationToken cancellationToken = default)
    {
        if (blockSize < MinBlockSize)
            throw new ArgumentOutOfRangeException($"Min block size is {MinBlockSize}.", nameof(MinBlockSize));

        var signResult = await SignSeedRequestAsync(request, accessKey, privateKey, blockSize, cancellationToken)
            .ConfigureAwait(false);

        // start consuming the data payload in blocks which we subsequently chunk; this prefixes
        // the data with a 'chunk header' containing signature data from the prior chunk (or header
        // signing, if the first chunk) plus length and other data. Each completed chunk is
        // written to the request stream and to complete the upload, we send a final chunk with
        // a zero-length data payload.

        // get the request stream and start writing the user data as chunks, as outlined
        // above; as
        var buffer = new byte[blockSize];
        var content = await request.ReadBodyAsync(cancellationToken).ConfigureAwait(false);

        if (content is null)
            return RawChunks.Empty;

        using var inputStream = new MemoryStream(content);
        long bytesRead;
        
        var rawChunks = new List<ReadOnlyMemory<byte>>();
        var lastComputedSignature = signResult.Signature;

        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            var chunk = ConstructSignedChunk(bytesRead, buffer, signResult.Timestamp, lastComputedSignature, signResult.SigningKey, out lastComputedSignature);
            rawChunks.Add(chunk);
        }

        // last step is to send a signed zero-length chunk to complete the upload
        var finalChunk = ConstructSignedChunk(0, buffer, signResult.Timestamp, lastComputedSignature, signResult.SigningKey, out _);
        rawChunks.Add(finalChunk);

        var result = new RawChunks(rawChunks);
        return result;
    }

    /// <summary>
    /// Calculates the expanded payload size of our data when it is chunked
    /// </summary>
    /// <param name="originalLength">
    /// The true size of the data payload to be uploaded
    /// </param>
    /// <param name="maxChunkSize">
    /// The size of each chunk we intend to send; each chunk will be
    /// prefixed with signed header data, expanding the overall size
    /// by a determinable amount
    /// </param>
    /// <param name="logger"></param>
    /// <returns>
    /// The overall payload size to use as content-length on a chunked upload
    /// </returns>
    internal static long CalculateChunkedContentLength(long originalLength, long maxChunkSize, ILogger logger)
    {
        if (originalLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalLength));
            
        if (maxChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize));

        var chunkLength = CalculateChunkLength(maxChunkSize);
            
        var maxSizeChunks = originalLength / chunkLength.Body;
        var remainingBytes = originalLength % chunkLength.Body;

        var chunkedContentLength = maxSizeChunks * chunkLength.Total
                                   + (remainingBytes > 0 ? CalculateChunkTotalLength(remainingBytes) : 0)
                                   + CalculateChunkTotalLength(0);

        logger.LogDebug(
            "Computed chunked content length for original length {OriginalLength} bytes, chunk size {ChunkSize}KB is {ChunkContentLength} bytes",
            originalLength, chunkLength.Total / 1024, chunkedContentLength);

        return chunkedContentLength;
    }

    internal static ChunkLength CalculateChunkLength(long chunkSize)
    {
        if (chunkSize < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        if (chunkSize == 0)
            return ChunkLength.Zero;

        //var hexChunkSizeLenth = Math.Log(chunkSize, 16);
        //var minHexChunkSizeLenth = (long)Math.Floor(hexChunkSizeLenth);
        //var maxHexChunkSizeLenth = (long)Math.Ceiling(hexChunkSizeLenth);
            
        var bodyLength = CalculateChunkBodyLength(chunkSize);

        //if (Math.Log(chunkSize - bodyLength, 16) < minHexChunkSizeLenth)
        //    bodyLength = CalculateChunkBodyLength(chunkSize) - (maxHexChunkSizeLenth - minHexChunkSizeLenth);
        //else
        //    bodyLength = CalculateChunkBodyLength(chunkSize);

        return new ChunkLength(bodyLength);
    }

    /// <summary>
    /// Returns a chunk for upload consisting of the signed 'header' or chunk
    /// prefix plus the user data. The signature of the chunk incorporates the
    /// signature of the previous chunk (or, if the first chunk, the signature
    /// of the headers portion of the request).
    /// </summary>
    /// <param name="userDataLen">
    /// The length of the user data contained in userData
    /// </param>
    /// <param name="userData">
    /// Contains the user data to be sent in the upload chunk
    /// </param>
    /// <param name="dateTimeStamp"></param>
    /// <param name="lastComputedSignature"></param>
    /// <param name="signingKey"></param>
    /// <param name="computedSignature"></param>
    /// <returns>
    /// A new buffer of data for upload containing the chunk header plus user data
    /// </returns>
    private ReadOnlyMemory<byte> ConstructSignedChunk(
        long userDataLen,
        byte[] userData,
        string dateTimeStamp,
        string lastComputedSignature,
        byte[] signingKey,
        out string computedSignature)
    {
        // to keep our computation routine signatures simple, if the userData
        // buffer contains less data than it could, shrink it. Note the special case
        // to handle the requirement that we send an empty chunk to complete
        // our chunked upload.
        byte[] dataToChunk;
        if (userDataLen == 0)
            dataToChunk = FinalChunk;
        else
        {
            if (userDataLen < userData.Length)
            {
                // shrink the chunkdata to fit
                dataToChunk = new byte[userDataLen];
                Array.Copy(userData, 0, dataToChunk, 0, userDataLen);
            }
            else
                dataToChunk = userData;
        }

        var chunkHeader = new StringBuilder();

        // start with size of user data
        chunkHeader.Append(dataToChunk.Length.ToString("X"));

        var chunkSignature = ComputeChunkSignature(
            dateTimeStamp, lastComputedSignature, dataToChunk, signingKey, Logger);

        // cache the signature to include with the next chunk's signature computation
        computedSignature = chunkSignature;

        // construct the actual chunk, comprised of the non-signed extensions, the
        // 'headers' we just signed and their signature, plus a newline then copy
        // that plus the user's data to a payload to be written to the request stream
        chunkHeader.Append(ChunkSignatureHeader + chunkSignature);
        chunkHeader.Append(ClRf);

        Logger.LogTrace("Chunk header:\n{Header}", chunkHeader);

        try
        {
            var header = Encoding.UTF8.GetBytes(chunkHeader.ToString());
            var trailer = Encoding.UTF8.GetBytes(ClRf);
            var signedChunk = new byte[header.Length + dataToChunk.Length + trailer.Length];

            Array.Copy(header, 0, signedChunk, 0, header.Length);
            Array.Copy(dataToChunk, 0, signedChunk, header.Length, dataToChunk.Length);
            Array.Copy(trailer, 0, signedChunk, header.Length + dataToChunk.Length, trailer.Length);

            // this is the total data for the chunk that will be sent to the request stream
            return signedChunk;
        }
        catch (Exception e)
        {
            throw new Exception("Unable to sign the chunked data. " + e.Message, e);
        }
    }

    static string ComputeChunkSignature(
        string dateTimeStamp,
        string lastComputedSignature,
        byte[] data,
        byte[] signingKey,
        ILogger logger)
    {
        // if this is the first chunk, we package it with the signing result
        // of the request headers, otherwise we use the cached signature
        // of the previous chunk

        using var hashAlgorithm = CreateCanonicalRequestHashAlgorithm();

        // sig-extension
        var chunkStringToSign =
            ChunkStringToSignPrefix + "\n" +
            dateTimeStamp + "\n" +
            lastComputedSignature + "\n" +
            hashAlgorithm.ComputeHash(data).ToHexString(Casing.Lower);

        // compute the V4 signature for the chunk
        var chunkSignature = ComputeKeyedHash(HmacSha256,
            signingKey,
            Encoding.UTF8.GetBytes(chunkStringToSign)).ToHexString(Casing.Lower);

        logger.LogTrace("Chunk string to sign:\n{ChunkStringToSign}\nChunk signature:\n{ChunkSignature}",
            chunkStringToSign, chunkSignature);

        return chunkSignature;
    }

    static long CalculateChunkTotalLength(long bodySize)
    {
        return bodySize.ToString("X").Length
               + ChunkSignatureHeader.Length
               + SignatureLength
               + ClRf.Length
               + bodySize
               + ClRf.Length;
    }

    static long CalculateChunkBodyLength(long maxChunkSize)
    {
        return maxChunkSize
               - maxChunkSize.ToString("X").Length
               - ChunkSignatureHeader.Length
               - SignatureLength
               - ClRf.Length
               //+ chunkSize
               - ClRf.Length;
    }

    protected override async Task<byte[]?> ValidateSignatureAsync(IHttpRequest request, AuthData authData, string privateKey, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (authData is null)
            throw new ArgumentNullException(nameof(authData));

        if (!request.IsChunked())
            return await base.ValidateSignatureAsync(request, authData, privateKey, cancellationToken)
                .ConfigureAwait(false);

        //

        Chunk? chunk;
        byte[]? contentBytes = null;
        var chunks = new List<Chunk>();

        var rawContentBytes = await request.ReadBodyAsync(cancellationToken).ConfigureAwait(false);
        if (rawContentBytes != null)
        {
            using var memoryStream = new MemoryStream(rawContentBytes);
            memoryStream.Position = 0;

            var streamReader = new StreamReader(memoryStream);
            

            while ((chunk = ReadChunk(streamReader)) != null)
                chunks.Add(chunk);

            var content = string.Join(string.Empty, chunks.Select(i => i.Data));
            contentBytes = Encoding.UTF8.GetBytes(content);
        }

        //

        var requestUri = request.RequestUri;
        var httpMethod = request.Method;
        //var bodyHash = ComputeBodyHash(contentBytes);
        var dateTime = ParseUtcDateTime(authData.Timestamp);
        var signedHeaderNames = ParseHeaderNames(authData.SignedHeaders);
        var headers = request.Headers.ToReadOnlyDictionary();
        var signedHeaders = GetHeaders(headers, signedHeaderNames).ToReadOnlyDictionary();

        var computeSignatureResult = ComputeSignature(
            requestUri,
            httpMethod,
            signedHeaders,
            StreamingBodySha256,
            authData.Credential,
            privateKey,
            dateTime,
            Logger);

        if (computeSignatureResult.Signature != authData.Signature)
            throw new InvalidOperationException("Invalid signature.");

        var lastSignature = computeSignatureResult.Signature;

        for (var i = 0; i < chunks.Count; i++)
        {
            chunk = chunks[i];
            var chunkData = Encoding.UTF8.GetBytes(chunk.Data);
            lastSignature = ComputeChunkSignature(computeSignatureResult.Timestamp, lastSignature, chunkData, computeSignatureResult.SigningKey, Logger);

            if (lastSignature != chunk.Header.Signature)
                throw new InvalidOperationException($"Invalid chunk #{i} signature.");
        }

        return contentBytes;
    }

    private async Task<ComputeSignatureResult> SignSeedRequestAsync(IHttpRequest request, string accessKey, string privateKey, long blockSize, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var body = await request.ReadBodyAsync(cancellationToken).ConfigureAwait(false);
        var content = body != null ? Encoding.UTF8.GetString(body) : string.Empty; 

        request.Headers.Set(XNDContentSHA256, StreamingBodySha256);
        request.Headers.Set(HeaderNames.ContentEncoding, ContentEncodingNwsChunked);
        request.Headers.Set(HeaderNames.ContentType, MediaTypeNames.Text.Plain);
        request.Headers.Set(XNDDecodedContentLength, content.Length.ToString(CultureInfo.InvariantCulture));

        var totalLength = CalculateChunkedContentLength(content.Length, blockSize, Logger);

        request.Headers.Set(HeaderNames.ContentLength, totalLength.ToString(CultureInfo.InvariantCulture));

        var computeSignatureResult = ComputeSignature(
            request.RequestUri,
            request.Method,
            request.Headers,
            StreamingBodySha256,
            accessKey,
            privateKey);

        var authData = new AuthData(computeSignatureResult);
        AuthDataSerializer.Write(request, authData);

        return computeSignatureResult;
    }

    private static Chunk? ReadChunk(StreamReader reader)
    {
        if (reader.EndOfStream)
            return default;

        var data = reader.ReadLine();
        if (data is null)
            return null;
        
        var pattern = $"^([0-9a-fA-F]+){ChunkSignatureHeader}([0-9a-fA-F]{{{SignatureLength}}})$";
        var match = Regex.Match(data, pattern);

        if (!match.Success)
            throw new FormatException("Invalid chunk format.");

        var lengthHexString = match.Groups[1].Value;
        var signature = match.Groups[2].Value;

        var length = long.Parse(lengthHexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var chunkHeader = new ChunkHeader(length, signature);

        var chunkContent = reader.ReadLine();
        if (chunkContent is null)
            return null;
        
        if (chunkContent.Length != chunkHeader.Length)
            throw new FormatException("Invalid chunk content length.");

        var chunk = new Chunk(chunkHeader, chunkContent);

        return chunk;
    }

    sealed class Chunk
    {
        public Chunk(ChunkHeader header, string data)
        {
            Header = header;
            Data = data;
        }

        public ChunkHeader Header { get; }
        public string Data { get; }
    }

    sealed class ChunkHeader
    {
        public ChunkHeader(long length, string signature)
        {
            Length = length;
            Signature = signature;
        }

        public long Length { get; }
        public string Signature { get; }
    }

    internal struct ChunkLength : IEquatable<ChunkLength>
    {
        public static readonly ChunkLength Zero = new ChunkLength();

        public ChunkLength(long body)
        {
            if (body < 0)
                throw new ArgumentOutOfRangeException(nameof(body));

            Body = body;
            Total = GetTotalLength(body);
        }

        public long Body { get; }
        public long Total { get; }

        public override int GetHashCode()
            => HashCode.Combine(Body, Total);

        public override bool Equals(object? obj)
            => obj is ChunkLength len && Equals(len);

        public bool Equals(ChunkLength other)
            => Body == other.Body && Total == other.Total;

        private static long GetTotalLength(long bodyLength)
            => bodyLength.ToString("X").Length
               + ChunkSignatureHeader.Length
               + SignatureLength
               + ClRf.Length
               + bodyLength
               + ClRf.Length;
    }
        
    private static AuthorizationHeaderChunkedSignerOptions CreateDefaultOptions()
        => new AuthorizationHeaderChunkedSignerOptions();
}
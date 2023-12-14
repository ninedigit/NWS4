using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NineDigit.NWS4;

public abstract class SignerOptions
{
}
    
public abstract class Signer<TAuthDataSerializer, TOptions> : Signer
    where TAuthDataSerializer : class, IAuthDataSerializer
{
    private protected Signer(
        SignerOptions options,
        IDateTimeProvider dateTimeProvider,
        IAuthDataSerializer authDataSerializer)
        : this(options, dateTimeProvider, authDataSerializer, NullLogger<Signer<TAuthDataSerializer, TOptions>>.Instance)
    {
    }
        
    private protected Signer(
        SignerOptions options,
        IDateTimeProvider dateTimeProvider,
        IAuthDataSerializer authDataSerializer,
        ILogger<Signer<TAuthDataSerializer, TOptions>> logger)
        : base(options, dateTimeProvider, authDataSerializer, logger)
    {
    }

    public new TAuthDataSerializer AuthDataSerializer
        => (TAuthDataSerializer)base.AuthDataSerializer;
}

// http://www.piotrwalat.net/hmac-authentication-in-asp-net-web-api/
// http://docs.aws.amazon.com/AmazonS3/latest/API/sig-v4-header-based-auth.html
public abstract class Signer
{
    //private static readonly Lazy<HashAlgorithm> lazyCanonicalRequestHashAlgorithm;

    public const string SchemeName = Scheme + "-" + Algorithm;

    protected const string Scheme = "NWS4";
    protected const string Algorithm = "HMAC-SHA256";

    private const string CanonicalRequestHashAlgorithmName = "SHA-256";

    // SHA256 hash of an empty request body
    public const string EmptyBodySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    // the name of the keyed hash algorithm used in signing
    protected const string HmacSha256 = "HMACSHA256";

    // format strings for the date/time and date stamps required during signing
    protected const string Iso8601Format = "yyyy-MM-ddTHH:mm:ss.fffK";

    // format strings for the date/time and date stamps required during signing
    protected const string DateFormat = "yyyy-MM-dd";

    // request canonicalization requires multiple whitespace compression
    private static readonly Regex CompressWhitespaceRegex = new Regex("\\s+");
        
    // some common x-nd-* parameters
    public const string XNDContentSHA256 = "x-nd-content-sha256";
    public const string XNDDate = "x-nd-date";
    public const string XNDDecodedContentLength = "X-NWS-Decoded-Content-Length";

    // algorithm used to hash the canonical request that is supplied to
    // the signature computation
    protected static HashAlgorithm CreateCanonicalRequestHashAlgorithm()
        => CreateHashAlgorithm(CanonicalRequestHashAlgorithmName);

    internal protected static string FormatDateTime(DateTime dateTimeStamp)
        => dateTimeStamp.ToString(Iso8601Format, CultureInfo.InvariantCulture);

    protected static string FormatDate(DateTime dateTimeStamp)
        => dateTimeStamp.ToString(DateFormat, CultureInfo.InvariantCulture);

    internal static DateTime ParseDateTime(string dateTime)
        => DateTime.ParseExact(dateTime, Iso8601Format, CultureInfo.InvariantCulture);

    protected static DateTime ParseDate(string date)
        => DateTime.ParseExact(date, DateFormat, CultureInfo.InvariantCulture);

    private protected Signer(
        SignerOptions options,
        IDateTimeProvider dateTimeProvider,
        IAuthDataSerializer authDataSerializer)
        : this(options, dateTimeProvider, authDataSerializer, NullLogger<Signer>.Instance)
    {
    }

    private protected Signer(
        SignerOptions options,
        IDateTimeProvider dateTimeProvider,
        IAuthDataSerializer authDataSerializer,
        ILogger<Signer> logger)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        DateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        AuthDataSerializer = authDataSerializer ?? throw new ArgumentNullException(nameof(authDataSerializer));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal SignerOptions Options { get; }
        
    internal ILogger<Signer> Logger { get; }
    internal IDateTimeProvider DateTimeProvider { get; }
    public IAuthDataSerializer AuthDataSerializer { get; }

    public async Task<byte[]?> ValidateSignatureAsync(
        IHttpRequest request,
        string privateKey,
        TimeSpan requestTimeWindow,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (requestTimeWindow.Milliseconds < -1)
            throw new ArgumentOutOfRangeException(nameof(requestTimeWindow));

        var authData = AuthDataSerializer.Read(request);

        if (authData is null)
            throw new ArgumentException("No authorization header.", nameof(request));

        if (authData.Scheme != SchemeName)
            throw new ArgumentException("Authorization header is not of NWS4 header type.", nameof(request));

        var content = await ValidateSignatureAsync(request, authData, privateKey, cancellationToken)
            .ConfigureAwait(false);

        var nowTimeStamp = DateTimeProvider.UtcNow;
        var timeStamp = authData.GetUtcDateTime();
        var isOutOfTimeWindow = requestTimeWindow != Timeout.InfiniteTimeSpan &&
                                (timeStamp < nowTimeStamp.Add(-requestTimeWindow) ||
                                 timeStamp > nowTimeStamp.Add(requestTimeWindow));

        if (isOutOfTimeWindow)
            throw new SignatureExpiredException($"Signature expired.");

        return content;
    }

    protected abstract Task<byte[]?> ValidateSignatureAsync(
        IHttpRequest request,
        AuthData authData,
        string privateKey,
        CancellationToken cancellationToken = default);

    private static HashAlgorithm CreateHashAlgorithm(string hashName)
    {
        var hashAlgorithm = HashAlgorithm.Create(hashName);

        if (hashAlgorithm is null)
            throw new ArgumentException($"Unable to create hash algorithm '{hashName}'.");

        return hashAlgorithm;
    }

    /// <summary>
    /// Returns the canonical collection of header names that will be included in
    /// the signature. For NWS4, all header names must be included in the process 
    /// in sorted canonicalized order.
    /// </summary>
    /// <param name="headers">
    /// The set of header names and values that will be sent with the request
    /// </param>
    /// <returns>
    /// The set of header names canonicalized to a flattened, ;-delimited string
    /// </returns>
    protected static string CanonicalizeHeaderNames(IReadOnlyDictionary<string, string> headers)
    {
        var headersToSign = GetHeaderNamesToSign(headers);
        var result = CanonicalizeHeaderNames(headersToSign);
        return result;
    }

    protected static string[] GetHeaderNamesToSign(IReadOnlyDictionary<string, string> headers)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        var headersToSign = new List<string>(headers.Keys);
        headersToSign.Sort(StringComparer.OrdinalIgnoreCase);

        var result = headersToSign.ToArray();
        return result;
    }

    /// <summary>
    /// Returns the canonical collection of header names that will be included in
    /// the signature. For NWS1, all header names must be included in the process 
    /// in sorted canonicalized order.
    /// </summary>
    /// <param name="headerNames">
    /// The set of header names that will be sent with the request
    /// </param>
    /// <returns>
    /// The set of header names canonicalized to a flattened, ;-delimited string
    /// </returns>
    protected static string CanonicalizeHeaderNames(string[] headerNames)
    {
        if (headerNames == null)
            throw new ArgumentNullException(nameof(headerNames));

        var names = headerNames.ToList();
        var sb = new StringBuilder();

        names.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (var header in names)
        {
            if (sb.Length > 0)
                sb.Append(';');

            sb.Append(header.ToLower());
        }

        var result = sb.ToString();
        return result;
    }

    protected static string[] ParseHeaderNames(string canonicalizedHeaderNames)
    {
        if (canonicalizedHeaderNames == null)
            throw new ArgumentNullException(nameof(canonicalizedHeaderNames));

        return canonicalizedHeaderNames.Length == 0 ?
            Array.Empty<string>() :
            canonicalizedHeaderNames.Split(';');
    }

    protected static string CanonicalizeQueryParameters(string? queryParameters)
    {
        var canonicalizedQueryParameters = string.Empty;
            
        if (string.IsNullOrEmpty(queryParameters))
            return canonicalizedQueryParameters;
            
        var paramCollection = queryParameters!
            .Split('&')
            .Select(p => p.Split('='))
            .Select(parts => new KeyValuePair<string, string>(parts[0], parts.Length > 1 ? parts[1] : ""))
            .ToList();

        canonicalizedQueryParameters = CanonicalizeQueryParameters(paramCollection);

        return canonicalizedQueryParameters;
    }

    protected static string CanonicalizeQueryParameters(IReadOnlyList<KeyValuePair<string, string>>? queryParameters)
    {
        queryParameters ??= new Collection<KeyValuePair<string, string>>();
            
        var canonicalizedQueryParameters = queryParameters.OrderBy(i => i.Key, StringComparer.Ordinal)
            .Select(kv => new KeyValuePair<string, string>(HttpHelper.UrlEncode(kv.Key), HttpHelper.UrlEncode(kv.Value)))
            .Select(kv => $"{kv.Key}={kv.Value}")
            .Aggregate((x, y) => $"{x}&{y}");

        return canonicalizedQueryParameters;
    }

    /// <summary>
    /// Computes the canonical headers with values for the request. 
    /// For NWS4, all headers must be included in the signing process.
    /// </summary>
    /// <param name="headers">The set of headers to be encoded</param>
    /// <returns>Canonicalized string of headers with values</returns>
    protected static string CanonicalizeHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0)
            return string.Empty;

        // step1: sort the headers into lower-case format; we create a new
        // map to ensure we can do a subsequent key lookup using a lower-case
        // key regardless of how 'headers' was created.
        var sortedHeaderMap = new SortedDictionary<string, string>(StringComparer.Ordinal);
            
        foreach (var header in headers.Keys)
            sortedHeaderMap.Add(header.ToLower(), headers[header]);

        // step2: form the canonical header:value entries in sorted order. 
        // Multiple white spaces in the values should be compressed to a single 
        // space.
        var sb = new StringBuilder();
        foreach (var header in sortedHeaderMap.Keys)
        {
            var headerValue = CompressWhitespaceRegex.Replace(sortedHeaderMap[header], " ");
            sb.AppendFormat("{0}:{1}\n", header, headerValue.Trim());
        }

        // TODO: Fix \n at the end
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Returns the canonical request string to go into the signer process; this 
    /// consists of several canonical sub-parts.
    /// </summary>
    /// <param name="endpointUri"></param>
    /// <param name="httpMethod"></param>
    /// <param name="canonicalizedQueryParameters"></param>
    /// <param name="canonicalizedHeaderNames">
    /// The set of header names to be included in the signature, formatted as a flattened, ;-delimited string
    /// </param>
    /// <param name="canonicalizedHeaders">
    /// </param>
    /// <param name="bodyHash">
    /// Precomputed SHA256 hash of the request body content. For chunked encoding this
    /// should be the fixed string ''.
    /// </param>
    /// <returns>String representing the canonicalized request for signing</returns>
    public static string CanonicalizeRequest(Uri? endpointUri,
        string httpMethod,
        string canonicalizedQueryParameters,
        string canonicalizedHeaderNames,
        string canonicalizedHeaders,
        string bodyHash)
    {
        var canonicalRequest = new StringBuilder();

        canonicalRequest.AppendFormat("{0}\n", httpMethod);
        canonicalRequest.AppendFormat("{0}\n", CanonicalResourcePath(endpointUri));
        canonicalRequest.AppendFormat("{0}\n", canonicalizedQueryParameters);

        canonicalRequest.AppendFormat("{0}\n", canonicalizedHeaders);
        canonicalRequest.AppendFormat("{0}\n", canonicalizedHeaderNames);

        canonicalRequest.Append(bodyHash);

        return canonicalRequest.ToString();
    }

    /// <summary>
    /// Returns the canonicalized resource path for the service endpoint
    /// </summary>
    /// <param name="endpointUri">Endpoint to the service/resource</param>
    /// <returns>Canonicalized resource path for the endpoint</returns>
    private static string CanonicalResourcePath(Uri? endpointUri)
    {
        if (endpointUri is null || string.IsNullOrEmpty(endpointUri.AbsolutePath))
            return "/";

        // encode the path per RFC3986
        return HttpHelper.UrlEncode(endpointUri.AbsolutePath, true);
    }

    /// <summary>
    /// Compute and return the multi-stage signing key for the request.
    /// </summary>
    /// <param name="algorithm">Hashing algorithm to use</param>
    /// <param name="secretAccessKey">The clear-text NWS secret key</param>
    /// <param name="date">Date of the request, in ISO format</param>
    /// <returns>Computed signing key</returns>
    protected static byte[] DeriveSigningKey(string algorithm, string secretAccessKey, string date)
    {
        var ksecret = (Scheme + secretAccessKey).ToCharArray();
        var result = ComputeKeyedHash(algorithm, Encoding.UTF8.GetBytes(ksecret), Encoding.UTF8.GetBytes(date));
        return result;
    }

    /// <summary>
    /// Compute and return the hash of a data blob using the specified algorithm
    /// and key
    /// </summary>
    /// <param name="algorithm">Algorithm to use for hashing</param>
    /// <param name="key">Hash key</param>
    /// <param name="data">Data blob</param>
    /// <exception cref="ArgumentException">When algorithm, specified by <paramref name="algorithm"/>, could not be created.</exception>
    /// <returns>Hash of the data</returns>
    protected static byte[] ComputeKeyedHash(string algorithm, byte[] key, byte[] data)
    {
        var kha = CreateKeyedHashAlgorithm(algorithm);

        kha.Key = key;
        var hash = kha.ComputeHash(data);

        return hash;
    }

    protected static KeyedHashAlgorithm CreateKeyedHashAlgorithm(string algorithm)
    {
        var kha = KeyedHashAlgorithm.Create(algorithm);

        if (kha is null)
            throw new ArgumentException($"Unable co create Keyed Hash Algorithm '{algorithm}'.");

        return kha;
    }

    public static string ComputeBodyHash(byte[]? content)
    {
        if (content is null || content.Length == 0)
            return EmptyBodySha256;
        
        // precompute hash of the body content
        using var hashAlgorithm = CreateCanonicalRequestHashAlgorithm();
        var contentHash = hashAlgorithm.ComputeHash(content);
        var bodyHash = contentHash.ToHexString(Casing.Lower);

        return bodyHash;
    }

    protected static IDictionary<string, string> GetHeaders(
        IReadOnlyDictionary<string, string> headers,
        string[] headerNames)
    {
        if (headers is null)
            throw new ArgumentNullException(nameof(headers));

        if (headerNames is null)
            throw new ArgumentNullException(nameof(headerNames));

        headerNames = headerNames.Distinct().ToArray();

        var result = new Dictionary<string, string>();

        for (var i = 0; i < headerNames.Length; i++)
        {
            var headerName = headerNames[i];
            var header = headers.FirstOrNull(h => StringComparer.OrdinalIgnoreCase.Equals(h.Key, headerName));
                
            if (header is null)
                throw new InvalidOperationException($"Header key '{headerName}' does not exist.");
                
            result.Add(headerName, header.Value.Value);
        }

        return result;
    }
}
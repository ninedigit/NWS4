using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NineDigit.NWS4;

// https://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-auth-using-authorization-header.html
public class AuthorizationHeaderSigner : Signer<AuthorizationHeaderAuthDataSerializer, AuthorizationHeaderSignerOptions>
{
    public AuthorizationHeaderSigner()
        : this(DefaultDateTimeProvider.Instance)
    { }

    public AuthorizationHeaderSigner(IDateTimeProvider dateTimeProvider)
        : this(dateTimeProvider, NullLogger<AuthorizationHeaderSigner>.Instance)
    {
    }
        
    public AuthorizationHeaderSigner(
        IDateTimeProvider dateTimeProvider,
        ILogger<AuthorizationHeaderSigner> logger)
        : this(CreateDefaultOptions(), dateTimeProvider, logger)
    {
    }

    public AuthorizationHeaderSigner(AuthorizationHeaderSignerOptions options)
        : this(options, DefaultDateTimeProvider.Instance)
    {
    }
        
    public AuthorizationHeaderSigner(
        AuthorizationHeaderSignerOptions options,
        ILogger<AuthorizationHeaderSigner> logger)
        : this(options, DefaultDateTimeProvider.Instance, logger)
    {
    }

    public AuthorizationHeaderSigner(
        AuthorizationHeaderSignerOptions options,
        IDateTimeProvider dateTimeProvider)
        : this(options, dateTimeProvider, NullLogger<AuthorizationHeaderSigner>.Instance)
    {
    }
        
    public AuthorizationHeaderSigner(
        AuthorizationHeaderSignerOptions options,
        IDateTimeProvider dateTimeProvider,
        ILogger<AuthorizationHeaderSigner> logger)
        : base(options, dateTimeProvider, new AuthorizationHeaderAuthDataSerializer(), logger)
    {
    }

    internal new AuthorizationHeaderSignerOptions Options
        => (AuthorizationHeaderSignerOptions)base.Options;
        
    public async Task SignRequestAsync(
        IHttpRequest request,
        string accessKey,
        string privateKey,
        CancellationToken cancellationToken = default)
    {
        var computeSignatureResult = await this
            .ComputeSignatureAsync(request, accessKey, privateKey, cancellationToken);

        var authData = new AuthData(computeSignatureResult);
        AuthDataSerializer.Write(request, authData);
    }
        
    internal async Task<ComputeSignatureResult> ComputeSignatureAsync(
        IHttpRequest request,
        string accessKey,
        string privateKey,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var body = await request.ReadBodyAsync(cancellationToken).ConfigureAwait(false);
        var bodyHash = ComputeBodyHash(body);
            
        var computeSignatureResult = this
            .ComputeSignature(request.RequestUri, request.Method, request.Headers, bodyHash, accessKey, privateKey);

        return computeSignatureResult;
    }

    public static ComputeSignatureResult ComputeSignature(
        DateTime now,
        Uri? uri,
        string httpMethod,
        IReadOnlyHttpRequestHeaders headers,
        string bodyHash,
        string accessKey,
        string privateKey)
        => ComputeSignature(now, uri, httpMethod, headers, bodyHash, accessKey, privateKey,
            NullLogger<AuthorizationHeaderSigner>.Instance);

    public static ComputeSignatureResult ComputeSignature(
        DateTime now,
        Uri? uri,
        string httpMethod,
        IReadOnlyHttpRequestHeaders headers,
        string bodyHash,
        string accessKey,
        string privateKey,
        ILogger logger)
    {
        var dateTime = now.ToUniversalTime();
        var dateTimeStamp = FormatDateTime(dateTime);

        // update the headers with required 'x-nd-content-sha256', 'x-nd-date' and 'host' values

        headers.RequireValue(XNDDate, dateTimeStamp, StringComparison.Ordinal);
        headers.RequireValue(XNDContentSHA256, bodyHash, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(headers.FindHost()) && uri != null)
        {
            var hostHeader = uri.Host;

            if (!uri.IsDefaultPort)
                hostHeader += ":" + uri.Port;

            headers.RequireValue(HeaderNames.Host, hostHeader, StringComparison.OrdinalIgnoreCase);
        }

        var headersDict = headers.ToDictionary();
        var result = ComputeSignature(uri, httpMethod, headersDict, bodyHash, accessKey, privateKey, dateTime, logger);

        return result;
    }

    private protected ComputeSignatureResult ComputeSignature(
        Uri? uri,
        string httpMethod,
        IHttpRequestHeaders headers,
        string bodyHash,
        string accessKey,
        string privateKey)
    {
        var dateTime = DateTimeProvider.UtcNow.ToUniversalTime();

        // update the headers with required 'x-nd-content-sha256', 'x-nd-date' and 'host' values

        headers.SetXNDDate(dateTime);
        headers.SetXNDContentSHA256(bodyHash);
        headers.TrySetHost(uri);

        var result = ComputeSignature(dateTime, uri, httpMethod, headers, bodyHash, accessKey, privateKey);

        return result;
    }

    private protected static ComputeSignatureResult ComputeSignature(
        Uri? requestUri,
        string httpMethod,
        IReadOnlyDictionary<string, string> headers,
        string bodyHash,
        string accessKey,
        string privateKey,
        DateTime dateTime,
        ILogger logger)
    {
        string? queryParameters = requestUri?.Query.TrimStart('?');

        // TODO: Validate required Headers like Host

        // canonicalize the headers; we need the set of header names as well as the
        // names and values to go into the signature process
        string canonicalizedHeaderNames = CanonicalizeHeaderNames(headers);
        string canonicalizedHeaders = CanonicalizeHeaders(headers);

        // if any query string parameters have been supplied, canonicalize them
        string canonicalizedQueryParameters = CanonicalizeQueryParameters(queryParameters);

        // canonicalize the various components of the request
        string canonicalRequest = CanonicalizeRequest(requestUri, httpMethod,
            canonicalizedQueryParameters, canonicalizedHeaderNames,
            canonicalizedHeaders, bodyHash);

        // Compute signature

        // first get the date and time for the subsequent request, and convert
        // to ISO 8601 format for use in signature generation
        string dateTimeStamp = FormatDateTime(dateTime);
        string dateStamp = FormatDate(dateTime);

        byte[] canonicalRequestHashBytes;

        // generate a hash of the canonical request, to go into signature computation
        using (var hashAlgorithm = CreateCanonicalRequestHashAlgorithm())
        {
            var canonicalRequestBytes = Encoding.UTF8.GetBytes(canonicalRequest);
            canonicalRequestHashBytes = hashAlgorithm.ComputeHash(canonicalRequestBytes);
        }

        string canonicalRequestHashHexString = canonicalRequestHashBytes.ToHexString(Casing.Lower);

        // construct the string to be signed
        string stringToSign = string.Format("{0}-{1}\n{2}\n{3}\n{4}",
            Scheme, Algorithm, dateTimeStamp, accessKey, canonicalRequestHashHexString);

        // compute the signing key
        string signatureString;
        byte[] signingKey;

        using (var kha = CreateKeyedHashAlgorithm(HmacSha256))
        {
            signingKey = kha.Key = DeriveSigningKey(HmacSha256, privateKey, dateStamp);

            // compute the NWS4 signature and return it
            var signature = kha.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            signatureString = signature.ToHexString(Casing.Lower);
        }

        logger.LogTrace(
            "Canonical Request:\n\"{CanonicalRequest}\"\nString to sign:\n{StringToSign}\nSignature:\n{Signature}",
            canonicalRequest, stringToSign, signatureString);
            
        var result = new ComputeSignatureResult(
            scheme: SchemeName,
            publicKey: accessKey,
            signedHeaderNames: canonicalizedHeaderNames,
            timestamp: dateTimeStamp,
            signingKey: signingKey,
            signature: signatureString);

        return result;
    }

    public static bool CanValidateSignature(string authHeaderValue)
        => AuthenticationHeaderValue.TryParse(authHeaderValue, out AuthenticationHeaderValue? header)
           && CanValidateSignature(header);

    public static bool CanValidateSignature(AuthenticationHeaderValue header)
    {
        if (header is null)
            throw new ArgumentNullException(nameof(header));

        return header.Scheme == SchemeName;
    }

    protected override async Task<byte[]?> ValidateSignatureAsync(
        IHttpRequest request,
        AuthData authData,
        string privateKey,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (authData is null)
            throw new ArgumentNullException(nameof(authData));

        var requestUri = request.RequestUri;
        var httpMethod = request.Method;
        var signedHeaderNames = ParseHeaderNames(authData.SignedHeaders);
        var headers = request.Headers.ToDictionary();
            
        if (Options.AllowForwardedHostHeader &&
            headers.ContainsKey(HeaderNames.Host) &&
            headers.TryGetValue(Options.ForwardedHostHeaderName, out var forwardedHost))
        {
            headers[HeaderNames.Host] = forwardedHost;
        }
            
        var signedHeaders = GetHeaders(headers, signedHeaderNames).ToReadOnlyDictionary();
        var content = await request.ReadBodyAsync(cancellationToken).ConfigureAwait(false);
        var bodyHash = ComputeBodyHash(content);
        var dateTime = ParseUtcDateTime(authData.Timestamp);

        var result = ComputeSignature(
            requestUri, httpMethod, signedHeaders,
            bodyHash, authData.Credential, privateKey, dateTime, Logger);

        if (result.Signature != authData.Signature)
            throw new InvalidOperationException("Invalid signature.");

        return content;
    }

    internal static DateTime ParseUtcDateTime(string dateTimeStamp)
        => ParseDateTime(dateTimeStamp).ToUniversalTime();
        
    private static AuthorizationHeaderSignerOptions CreateDefaultOptions() => new();
}
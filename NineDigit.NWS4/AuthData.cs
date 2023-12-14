using System;

namespace NineDigit.NWS4;

public sealed class AuthData
{
    public AuthData(ComputeSignatureResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));
            
        Scheme = result.Scheme;
        Credential = result.PublicKey;
        SignedHeaders = result.SignedHeaderNames;
        Timestamp = result.Timestamp;
        Signature = result.Signature;
    }

    public AuthData(string scheme, string credential, string signedHeaders, string timestamp, string signature)
    {
        Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
        Credential = credential ?? throw new ArgumentNullException(nameof(credential));
        SignedHeaders = signedHeaders ?? throw new ArgumentNullException(nameof(signedHeaders));
        Timestamp = timestamp ?? throw new ArgumentNullException(nameof(timestamp));
        Signature = signature ?? throw new ArgumentNullException(nameof(signature));
    }

    public string Scheme { get; }
    public string Credential { get; }
    public string SignedHeaders { get; }
    /// <summary>
    /// ISO format date time stamp
    /// </summary>
    public string Timestamp { get; }
    public string Signature { get; }

    public DateTime GetUtcDateTime()
        => AuthorizationHeaderSigner.ParseUtcDateTime(Timestamp);
}
using System.Linq;

namespace NineDigit.NWS4;

public sealed class ComputeSignatureResult
{
    private readonly byte[] _signingKey;

    internal ComputeSignatureResult(
        string scheme,
        string publicKey,
        string signedHeaderNames,
        string timestamp,
        byte[] signingKey,
        string signature
    )
    {
        Scheme = scheme;
        PublicKey = publicKey;
        SignedHeaderNames = signedHeaderNames;
        Timestamp = timestamp;
        Signature = signature;
        _signingKey = signingKey.ToArray();
    }

    public string Scheme { get; }
    public string PublicKey { get; }
    public string SignedHeaderNames { get; }
    public string Timestamp { get; }
    public string Signature { get; }
    public byte[] SigningKey
        => _signingKey.ToArray();
}
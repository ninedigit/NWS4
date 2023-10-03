using System.Linq;

namespace NineDigit.NWS4;

public sealed class ComputeSignatureResult
{
    private readonly byte[] signingKey;

    internal ComputeSignatureResult(
        string scheme,
        string publicKey,
        string signedHeaderNames,
        string timestamp,
        byte[] signingKey,
        string signature
    )
    {
        this.Scheme = scheme;
        this.PublicKey = publicKey;
        this.SignedHeaderNames = signedHeaderNames;
        this.Timestamp = timestamp;
        this.Signature = signature;
        this.signingKey = signingKey.ToArray();
    }

    public string Scheme { get; }
    public string PublicKey { get; }
    public string SignedHeaderNames { get; }
    public string Timestamp { get; }
    public string Signature { get; }
    public byte[] SigningKey
        => this.signingKey.ToArray();
}
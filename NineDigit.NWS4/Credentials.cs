using System;

namespace NineDigit.NWS4;

public sealed class Credentials
{
    public Credentials(string publicKey, string privateKey)
    {
        if (string.IsNullOrEmpty(publicKey))
            throw new ArgumentException("Value can not be null or empty.", nameof(publicKey));

        if (string.IsNullOrEmpty(privateKey))
            throw new ArgumentException("Value can not be null or empty.", nameof(privateKey));

        PublicKey = publicKey;
        PrivateKey = privateKey;
    }

    public string PublicKey { get; }
    public string PrivateKey { get; } // TODO: SecureString?
}
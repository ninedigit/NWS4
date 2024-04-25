using System;
using System.Security;

namespace NineDigit.NWS4;

public sealed class Credentials : IDisposable
{
    public Credentials(string publicKey, string privateKey)
        : this(publicKey, SecureStringHelper.CreateSecureString(privateKey))
    {
    }

    public Credentials(string publicKey, SecureString privateKey)
    {
        if (string.IsNullOrEmpty(publicKey))
            throw new ArgumentException("Value can not be null or empty.", nameof(publicKey));

        if (privateKey is null)
            throw new ArgumentNullException(nameof(privateKey));

        PublicKey = publicKey;
        PrivateKey = privateKey;
    }

    public string PublicKey { get; }
    public SecureString PrivateKey { get; }

    public void Dispose()
        => PrivateKey.Dispose();
}
#if NET6_0_OR_GREATER
using System;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public static class SignerExtensions
{
    public static Task<byte[]?> ValidateSignatureAsync(
        this Signer self,
        HttpRequestMessage requestMessage,
        SecureString privateKey,
        TimeSpan requestTimeWindow,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessageWrapper(requestMessage);
        var result = self.ValidateSignatureAsync(request, privateKey, requestTimeWindow, cancellationToken);

        return result;
    }
}
#endif
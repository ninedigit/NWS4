using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NineDigit.NWS4.AspNetCore;

public static class SignerExtensions
{
    public static Task<byte[]?> ValidateSignatureAsync(
        this Signer self,
        HttpRequest httpRequest,
        SecureString privateKey,
        TimeSpan requestTimeWindow,
        CancellationToken cancellationToken = default)
    {
        var request = new AspNetCoreHttpRequestWrapper(httpRequest);
        var result = self.ValidateSignatureAsync(request, privateKey, requestTimeWindow, cancellationToken);

        return result;
    }
}
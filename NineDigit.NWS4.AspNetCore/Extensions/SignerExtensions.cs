using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NineDigit.NWS4.AspNetCore;

public static class SignerExtensions
{
    public static Task<byte[]?> ValidateSignatureAsync(
        this Signer self,
        HttpRequestMessage requestMessage,
        string privateKey,
        TimeSpan requestTimeWindow,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessageWrapper(requestMessage);
        var result = self.ValidateSignatureAsync(request, privateKey, requestTimeWindow, cancellationToken);

        return result;
    }
        
    public static Task<byte[]?> ValidateSignatureAsync(
        this Signer self,
        HttpRequest httpRequest,
        string privateKey,
        TimeSpan requestTimeWindow,
        CancellationToken cancellationToken = default)
    {
        var request = new AspNetCoreHttpRequestWrapper(httpRequest);
        var result = self.ValidateSignatureAsync(request, privateKey, requestTimeWindow, cancellationToken);

        return result;
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NineDigit.NWS4.AspNetCore;

public static class AuthorizationHeaderSignerExtensions
{
    public static Task SignRequestAsync(
        this AuthorizationHeaderSigner self,
        HttpRequest request,
        Credentials credentials,
        CancellationToken cancellationToken = default)
    {
        var requestWrapper = new AspNetCoreHttpRequestWrapper(request);
        var result = self.SignRequestAsync(requestWrapper, credentials, cancellationToken);

        return result;
    }
}
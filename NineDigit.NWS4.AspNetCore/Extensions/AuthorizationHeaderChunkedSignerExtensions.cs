using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NineDigit.NWS4.AspNetCore;

public static class AuthorizationHeaderChunkedSignerExtensions
{
    public static Task SignRequestAsync(
        this AuthorizationHeaderChunkedSigner self,
        HttpRequest request,
        string accessKey,
        string privateKey,
        CancellationToken cancellationToken = default)
    {
        var requestWrapper = new AspNetCoreHttpRequestWrapper(request);
        var result = self.SignRequestAsync(requestWrapper, accessKey, privateKey, cancellationToken);

        return result;
    }
}
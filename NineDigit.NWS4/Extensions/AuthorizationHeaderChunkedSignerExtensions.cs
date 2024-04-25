#if NET6_0_OR_GREATER
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public static class AuthorizationHeaderChunkedSignerExtensions
{
    public static Task SignRequestAsync(
        this AuthorizationHeaderChunkedSigner self,
        HttpRequestMessage request,
        Credentials credentials,
        CancellationToken cancellationToken = default)
    {
        var requestWrapper = new HttpRequestMessageWrapper(request);
        var result = self.SignRequestAsync(requestWrapper, credentials, cancellationToken);

        return result;
    }
}
#endif
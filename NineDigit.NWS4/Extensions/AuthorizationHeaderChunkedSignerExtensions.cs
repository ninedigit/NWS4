using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public static class AuthorizationHeaderChunkedSignerExtensions
{
    public static Task SignRequestAsync(
        this AuthorizationHeaderChunkedSigner self,
        HttpRequestMessage request,
        string accessKey,
        string privateKey,
        CancellationToken cancellationToken = default)
    {
        var requestWrapper = new HttpRequestMessageWrapper(request);
        var result = self.SignRequestAsync(requestWrapper, accessKey, privateKey, cancellationToken);

        return result;
    }
}
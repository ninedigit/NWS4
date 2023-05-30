using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NineDigit.NWS4.AspNetCore
{
    public static class AuthorizationHeaderSignerExtensions
    {
        public static Task SignRequestAsync(
            this AuthorizationHeaderSigner self,
            HttpRequestMessage request,
            string accessKey,
            string privateKey,
            CancellationToken cancellationToken = default)
        {
            var requestWrapper = new HttpRequestMessageWrapper(request);
            var result = self.SignRequestAsync(requestWrapper, accessKey, privateKey, cancellationToken);

            return result;
        }

        public static Task SignRequestAsync(
            this AuthorizationHeaderSigner self,
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
}
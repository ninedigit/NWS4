using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore
{
    internal sealed class HttpRequestWrapper : IHttpRequest
    {
        private readonly IHttpRequest innerRequest;

        public HttpRequestWrapper(HttpRequestMessage requestMessage)
            : this(new HttpRequestMessageWrapper(requestMessage))
        {
        }

        public HttpRequestWrapper(Microsoft.AspNetCore.Http.HttpRequest httpRequest)
            : this(new AspNetCoreHttpRequestWrapper(httpRequest))
        {
        }

        public HttpRequestWrapper(IHttpRequest request)
        {
            this.innerRequest = request ?? throw new ArgumentNullException(nameof(request));
        }

        public Uri? RequestUri => innerRequest.RequestUri;
        public string Method => innerRequest.Method;
        public IHttpRequestHeaders Headers => innerRequest.Headers;

        public Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
            => innerRequest.ReadBodyAsync(cancellationToken);
    }
}

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

internal sealed class HttpRequestWrapper : IHttpRequest
{
    private readonly IHttpRequest _innerRequest;

    public HttpRequestWrapper(Microsoft.AspNetCore.Http.HttpRequest httpRequest)
        : this(new AspNetCoreHttpRequestWrapper(httpRequest))
    {
    }

    public HttpRequestWrapper(IHttpRequest request)
    {
        _innerRequest = request ?? throw new ArgumentNullException(nameof(request));
    }

    public Uri? RequestUri => _innerRequest.RequestUri;
    public string Method => _innerRequest.Method;
    public IHttpRequestHeaders Headers => _innerRequest.Headers;

    public Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
        => _innerRequest.ReadBodyAsync(cancellationToken);
}
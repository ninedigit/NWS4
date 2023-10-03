using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

internal sealed class AspNetCoreHttpRequestWrapper : IHttpRequest
{
    private readonly Microsoft.AspNetCore.Http.HttpRequest request;

    public AspNetCoreHttpRequestWrapper(Microsoft.AspNetCore.Http.HttpRequest request)
    {
        this.request = request ?? throw new ArgumentNullException(nameof(request));

        this.RequestUri = new Uri(UriHelper.GetEncodedUrl(request));
        this.Method = request.Method.ToUpperInvariant();
        this.Headers = new HeaderDictionaryWrapper(request.Headers);
    }

    public Uri? RequestUri { get; }

    public string Method { get; }

    public IHttpRequestHeaders Headers { get; }

    public async Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
    {
        var body = await this.request.PeekBodyAsync(cancellationToken)
            .ConfigureAwait(false);

        return body;
    }
}
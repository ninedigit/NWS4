using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

internal sealed class HttpRequestMessageWrapper : IHttpRequest
{
    private readonly HttpRequestMessage message;

    public HttpRequestMessageWrapper(HttpRequestMessage message)
    {
        this.message = message ?? throw new ArgumentNullException(nameof(message));
        this.Headers = new HttpRequestMessageHeadersWrapper(message.Headers);
    }

    public Uri? RequestUri
        => this.message.RequestUri;

    public string Method
        => this.message.Method.Method.ToUpperInvariant();

    public IHttpRequestHeaders Headers { get; }

    public Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
        => this.message.TryReadContentAsByteArrayAsync();
}
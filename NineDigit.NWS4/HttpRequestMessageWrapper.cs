using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

internal sealed class HttpRequestMessageWrapper : IHttpRequest
{
    private readonly HttpRequestMessage _message;

    public HttpRequestMessageWrapper(HttpRequestMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
        Headers = new HttpRequestMessageHeadersWrapper(message.Headers);
    }

    public Uri? RequestUri
        => _message.RequestUri;

    public string Method
        => _message.Method.Method.ToUpperInvariant();

    public IHttpRequestHeaders Headers { get; }

    public Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
        => _message.TryReadContentAsByteArrayAsync();
}
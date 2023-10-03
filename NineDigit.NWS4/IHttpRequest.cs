using System;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public interface IHttpRequest
{
    Uri? RequestUri { get; }
    string Method { get; }

    IHttpRequestHeaders Headers { get; }

    Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default);
}
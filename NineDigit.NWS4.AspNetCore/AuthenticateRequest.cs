using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

public sealed class AuthenticateRequest : IDisposable
{
    private byte[]? _body;
    private bool _disposed;
    private bool _validated;
    private readonly AsyncLock _bodySyncRoot;

    internal AuthenticateRequest(
        Signer signer,
        IHttpRequest request,
        AuthData authenticationHeaderContext,
        NWS4AuthenticationSchemeOptions options)
    {
        Signer = signer ?? throw new ArgumentNullException(nameof(signer));
        Request = request ?? throw new ArgumentNullException(nameof(request));
        AuthenticationHeaderContext = authenticationHeaderContext ?? throw new ArgumentNullException(nameof(authenticationHeaderContext));
        Options = options ?? throw new ArgumentNullException(nameof(options));

        _bodySyncRoot = new AsyncLock();
    }

    private Signer Signer { get; }
    public IHttpRequest Request { get; }
    public AuthData AuthenticationHeaderContext { get; }
    public NWS4AuthenticationSchemeOptions Options { get; }

    public async Task ValidateSignatureAsync(string privateKey, CancellationToken cancellationToken = default)
    {
        using (await _bodySyncRoot.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var content = await Signer
                .ValidateSignatureAsync(Request, privateKey, Options.RequestTimeWindow, cancellationToken)
                .ConfigureAwait(false);

            _body = content;
            _validated = true;
        }
    }

    public async Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
    {
        using (await _bodySyncRoot.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_validated)
                throw new InvalidOperationException("Content signature was not validated.");

            var result = _body?.ToArray();
            return result;
        }

    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                _bodySyncRoot.Dispose();

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
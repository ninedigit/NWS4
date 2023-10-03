using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

public sealed class AuthenticateRequest : IDisposable
{
    private byte[]? body;
    private bool disposed;
    private bool validated;
    private readonly AsyncLock bodySyncRoot;

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

        bodySyncRoot = new AsyncLock();
    }

    private Signer Signer { get; }
    public IHttpRequest Request { get; }
    public AuthData AuthenticationHeaderContext { get; }
    public NWS4AuthenticationSchemeOptions Options { get; }

    public async Task ValidateSignatureAsync(string privateKey, CancellationToken cancellationToken = default)
    {
        using (await this.bodySyncRoot.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var content = await this.Signer
                .ValidateSignatureAsync(this.Request, privateKey, this.Options.RequestTimeWindow, cancellationToken)
                .ConfigureAwait(false);

            this.body = content;
            this.validated = true;
        }
    }

    public async Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken = default)
    {
        using (await this.bodySyncRoot.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!this.validated)
                throw new InvalidOperationException("Content signature was not validated.");

            var result = this.body?.ToArray();
            return result;
        }

    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
                this.bodySyncRoot.Dispose();

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
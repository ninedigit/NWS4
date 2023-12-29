using System;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore;

internal sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _locker = new(1, 1);
    private bool _disposed;

    internal bool Locked
        => _locker.CurrentCount == 0;

    public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        await _locker.WaitAsync(cancellationToken).ConfigureAwait(false);
        var result = new ActionDisposable(() => _locker.Release());
        return result;
    }

    sealed class ActionDisposable : IDisposable
    {
        private bool _disposed;
        private readonly Action _action;

        public ActionDisposable(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _action();

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                _locker.Dispose();

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
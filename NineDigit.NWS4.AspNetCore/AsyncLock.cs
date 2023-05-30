using System;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4.AspNetCore
{
    internal sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private bool disposed;

        internal bool Locked
            => this.locker.CurrentCount == 0;

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await locker.WaitAsync(cancellationToken).ConfigureAwait(false);
            var result = new ActionDisposable(() => locker.Release());
            return result;
        }

        sealed class ActionDisposable : IDisposable
        {
            private bool disposed;
            private readonly Action action;

            public ActionDisposable(Action action)
            {
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                        this.action();

                    disposed = true;
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
            if (!disposed)
            {
                if (disposing)
                    this.locker.Dispose();

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

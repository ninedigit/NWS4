using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NineDigit.NWS4.AspNetCore.Tests
{
    public class AsyncLockTests
    {
        [Fact]
        public void LockAsync_CtorCreatesUnlockedInstance()
        {
            var asyncLock = new AsyncLock();

            Assert.False(asyncLock.Locked);
        }

        [Fact]
        public async Task LockAsync_LocksTheLock()
        {
            var asyncLock = new AsyncLock();
            await asyncLock.LockAsync(CancellationToken.None);

            Assert.True(asyncLock.Locked);
        }

        [Fact]
        public async Task LockAsync_DisposingLockHandleUnlocksTheLock()
        {
            var asyncLock = new AsyncLock();

            var handle = await asyncLock.LockAsync(CancellationToken.None);

            handle.Dispose();

            Assert.False(asyncLock.Locked);
        }
    }
}

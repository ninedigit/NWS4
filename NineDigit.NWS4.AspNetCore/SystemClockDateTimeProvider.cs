using Microsoft.AspNetCore.Authentication;
using System;

namespace NineDigit.NWS4.AspNetCore
{
    internal class SystemClockDateTimeProvider : IDateTimeProvider, ISystemClock
    {
        private readonly ISystemClock systemClock;

        public SystemClockDateTimeProvider(ISystemClock systemClock)
        {
            this.systemClock = systemClock
                ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public DateTime UtcNow
            => this.systemClock.UtcNow.UtcDateTime;

        DateTimeOffset ISystemClock.UtcNow
            => systemClock.UtcNow;
    }
}

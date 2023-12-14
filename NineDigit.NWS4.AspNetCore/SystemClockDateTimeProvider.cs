using Microsoft.AspNetCore.Authentication;
using System;

namespace NineDigit.NWS4.AspNetCore;

internal class SystemClockDateTimeProvider : IDateTimeProvider, ISystemClock
{
    private readonly ISystemClock _systemClock;

    public SystemClockDateTimeProvider(ISystemClock systemClock)
    {
        _systemClock = systemClock
                           ?? throw new ArgumentNullException(nameof(systemClock));
    }

    public DateTime UtcNow
        => _systemClock.UtcNow.UtcDateTime;

    DateTimeOffset ISystemClock.UtcNow
        => _systemClock.UtcNow;
}
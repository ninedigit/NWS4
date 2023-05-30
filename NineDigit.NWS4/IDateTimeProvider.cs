using System;

namespace NineDigit.NWS4
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}

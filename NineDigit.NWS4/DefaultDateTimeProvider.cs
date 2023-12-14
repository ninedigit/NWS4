using System;

namespace NineDigit.NWS4;

public sealed class DefaultDateTimeProvider : IDateTimeProvider
{
    public static DefaultDateTimeProvider Instance { get; } = new();

    private readonly Func<DateTime> _dateTimeFactory;

    public DefaultDateTimeProvider()
        : this(GetUtcNow)
    { }

    public DefaultDateTimeProvider(Func<DateTime> utcDateTimeFactory)
    {
        _dateTimeFactory = utcDateTimeFactory
                               ?? throw new ArgumentNullException(nameof(utcDateTimeFactory));
    }

    public DateTime UtcNow => _dateTimeFactory();

    private static DateTime GetUtcNow()
        => DateTime.UtcNow;

}
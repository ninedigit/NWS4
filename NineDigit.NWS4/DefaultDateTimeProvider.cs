﻿using System;

namespace NineDigit.NWS4;

public sealed class DefaultDateTimeProvider : IDateTimeProvider
{
    public static DefaultDateTimeProvider Instance { get; } = new();

    private readonly Func<DateTime> dateTimeFactory;

    public DefaultDateTimeProvider()
        : this(GetUtcNow)
    { }

    public DefaultDateTimeProvider(Func<DateTime> utcDateTimeFactory)
    {
        this.dateTimeFactory = utcDateTimeFactory
                               ?? throw new ArgumentNullException(nameof(utcDateTimeFactory));
    }

    public DateTime UtcNow => this.dateTimeFactory();

    private static DateTime GetUtcNow()
        => DateTime.UtcNow;

}
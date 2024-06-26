using System;
using System.Collections.Generic;

namespace NineDigit.NWS4;

internal static class ReadOnlyHttpRequestHeadersExtensions
{
    public static void RequireValue(
        this IReadOnlyHttpRequestHeaders headers,
        string key,
        string? value,
        StringComparison comparison = StringComparison.Ordinal)
    {
        if (!headers.TryGet(key, out string? headerValue))
            throw new KeyNotFoundException($"Header key '{key}' was not found.");

        var stringComparer =
#if NETSTANDARD2_0
            comparison.ToStringComparer();
        #else
            StringComparer.FromComparison(comparison);
#endif
        
        if (!stringComparer.Equals(headerValue, value))
            throw new InvalidOperationException($"Invalid value for header key '{key}'.");
    }
}
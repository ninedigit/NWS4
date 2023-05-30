using System;
using System.Collections.Generic;

namespace NineDigit.NWS4
{
    internal static class IReadOnlyHttpRequestHeadersExtensions
    {
        public static void RequireValue(
            this IReadOnlyHttpRequestHeaders headers,
            string key,
            string? value,
            StringComparison comparison = StringComparison.Ordinal)
        {
            if (!headers.TryGetValue(key, out string? headerValue))
                throw new KeyNotFoundException($"Header key '{key}' was not found.");

            if (!StringComparer.FromComparison(comparison).Equals(headerValue, value))
                throw new InvalidOperationException($"Invalid value for header key '{key}'.");
        }
    }
}
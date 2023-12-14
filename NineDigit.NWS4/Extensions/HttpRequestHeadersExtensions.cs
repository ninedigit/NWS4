using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NineDigit.NWS4;

public static class HttpRequestHeadersExtensions
{
    public static Dictionary<string, string> ToDictionary(this IReadOnlyHttpRequestHeaders headers)
        => headers.ToDictionary(i => i.Key, i => MergeValues(i.Value), StringComparer.OrdinalIgnoreCase);
        
    public static IReadOnlyDictionary<string, string> ToReadOnlyDictionary(this IReadOnlyHttpRequestHeaders headers)
    {
        var dictionary = headers.ToDictionary();
        var result = new ReadOnlyDictionary<string, string>(dictionary);
        
        return result;
    }

    public static void Set(this IHttpRequestHeaders self, string name, string? value)
    {
        self.Remove(name);
        self.Add(name, value);
    }

    public static bool TryGetValue(this IReadOnlyHttpRequestHeaders self, string name, out string? value)
    {
        if (self.TryGet(name, out IEnumerable<string?>? values))
        {
            value = MergeValues(values);
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public static bool TryGetValues(this IReadOnlyHttpRequestHeaders self, string name, out StringValues? result)
    {
        if (self.TryGet(name, out IEnumerable<string?>? values))
        {
            result = values != null ? new StringValues(values.ToArray()) : default(StringValues?);
            return true;
        }

        result = default;
        return false;
    }

    public static string? GetValuesOrNull(this IReadOnlyHttpRequestHeaders self, string name)
        => self.TryGetValue(name, out string? value) ? value : default;

    // public static StringValues? GetValuesOrNull(this IReadOnlyHttpRequestHeaders self, string name)
    //     => self.TryGetValues(name, out StringValues? value) ? value : default;

    public static string? FindAuthorization(this IReadOnlyHttpRequestHeaders self)
        => self.GetValuesOrNull(HeaderNames.Authorization);

    public static void SetAuthorization(this IHttpRequestHeaders self, string? value)
        => self.Set(HeaderNames.Authorization, value);

    public static string? FindHost(this IReadOnlyHttpRequestHeaders self)
        => self.GetValuesOrNull(HeaderNames.Host);

    public static bool TrySetHost(this IHttpRequestHeaders self, Uri? uri)
    {
        if (string.IsNullOrEmpty(self.FindHost()) && uri != null)
        {
            var hostHeader = uri.Host;

            if (!uri.IsDefaultPort)
                hostHeader += ":" + uri.Port;

            self.SetHost(hostHeader);
            return true;
        }

        return false;
    }
        
    public static void SetHost(this IHttpRequestHeaders self, string? value)
        => self.Set(HeaderNames.Host, value);

    public static void SetXNDDate(this IHttpRequestHeaders self, DateTime date)
        => self.Set(Signer.XNDDate, Signer.FormatDateTime(date.ToUniversalTime()));

    public static void SetXNDContentSHA256(this IHttpRequestHeaders self)
        => self.Set(Signer.XNDContentSHA256, Signer.EmptyBodySha256);
        
    public static void SetXNDContentSHA256(this IHttpRequestHeaders self, string bodyHash)
        => self.Set(Signer.XNDContentSHA256, bodyHash);
        
    private static string MergeValues(IEnumerable<string?> values)
        => string.Join(",", values.Where(i => i != null));
}
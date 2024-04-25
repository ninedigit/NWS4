using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4;

public class HttpRequestHeaders : IHttpRequestHeaders
{
    private readonly Dictionary<string, string> _entries;

    public HttpRequestHeaders()
    {
        _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
    
    public HttpRequestHeaders(IDictionary<string, string> collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        _entries = new Dictionary<string, string>(collection, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string name, [NotNullWhen(true)] out string? value)
        => _entries.TryGetValue(name, out value);

    public void Add(string name, string? value)
        => _entries.Add(name, value ?? string.Empty);

    public bool Remove(string name)
        => _entries.Remove(name);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
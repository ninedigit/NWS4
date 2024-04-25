using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4.AspNetCore;

internal class HeaderDictionaryWrapper : IHttpRequestHeaders
{
    private readonly IHeaderDictionary _headers;

    public HeaderDictionaryWrapper(IHeaderDictionary headers)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    public void Add(string name, string? value)
    {
        lock (_headers)
        {
            var stringValues = new StringValues(value);
            _headers.Append(name, stringValues);
        }
    }

    public bool Remove(string name)
    {
        lock (_headers)
            return _headers.Remove(name);
    }

    public bool TryGet(string name, [NotNullWhen(true)] out string? values)
    {
        lock (_headers)
        {
            if (_headers.TryGetValue(name, out StringValues stringValues))
            {
                values = stringValues.ToString();
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        lock (_headers)
        {
            foreach (var header in _headers)
                yield return new KeyValuePair<string, string>(header.Key, header.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
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

    public bool TryGet(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
    {
        lock (_headers)
        {
            if (_headers.TryGetValue(name, out StringValues stringValues))
            {
                values = stringValues.ToArray();
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }
    }

    public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
    {
        lock (_headers)
        {
            foreach (var key in _headers.Keys)
            {
                var values = _headers.GetCommaSeparatedValues(key);
                var result = new KeyValuePair<string, IEnumerable<string>>(key, values);

                yield return result;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
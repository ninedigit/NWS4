using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NineDigit.NWS4.AspNetCore;

internal sealed class HttpRequestHeadersWrapper : IHttpRequestHeaders
{
    private readonly IHeaderDictionary _headers;

    public HttpRequestHeadersWrapper()
    {
        _headers = new HeaderDictionary();
    }

    public HttpRequestHeadersWrapper(IHttpRequestHeaders headers)
    {
        if (headers is null)
            throw new ArgumentNullException(nameof(headers));

        _headers = new HeaderDictionary(headers.ToDictionary(i => i.Key, i => new StringValues(i.Value.ToArray())));
    }

    public HttpRequestHeadersWrapper(IReadOnlyHttpRequestHeaders headers)
    {
        if (headers is null)
            throw new ArgumentNullException(nameof(headers));

        _headers = new HeaderDictionary(headers.ToDictionary(i => i.Key, i => new StringValues(i.Value.ToArray())));
    }

    public void Add(string name, string? value)
    {
        lock (_headers)
            _headers.Add(name, value);
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
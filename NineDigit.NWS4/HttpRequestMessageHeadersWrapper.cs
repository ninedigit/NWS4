#if NET6_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4;

internal sealed class HttpRequestMessageHeadersWrapper : IHttpRequestHeaders
{
    private readonly System.Net.Http.Headers.HttpRequestHeaders _headers;

    public HttpRequestMessageHeadersWrapper(System.Net.Http.Headers.HttpRequestHeaders headers)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    public void Add(string name, string? value)
        => _headers.TryAddWithoutValidation(name, value);

    public bool Remove(string name)
        => _headers.Remove(name);

    public bool TryGet(string name, [NotNullWhen(true)] out string? value)
    {
        if (_headers.NonValidated.TryGetValues(name, out var values))
        {
            value = values.ToString();
            return true;
        }

        value = default;
        return false;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (var header in _headers.NonValidated)
            yield return new KeyValuePair<string, string>(header.Key, header.Value.ToString());
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
#endif
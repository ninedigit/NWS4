using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4.AspNetCore;

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

    public bool TryGet(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        => _headers.TryGetValues(name, out values);

    public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4.AspNetCore
{
    internal sealed class HttpRequestMessageHeadersWrapper : IHttpRequestHeaders
    {
        private readonly System.Net.Http.Headers.HttpRequestHeaders _headers;

        public HttpRequestMessageHeadersWrapper(System.Net.Http.Headers.HttpRequestHeaders headers)
        {
            this._headers = headers
                ?? throw new ArgumentNullException(nameof(headers));
        }

        public void Add(string name, string? value)
            => this._headers.TryAddWithoutValidation(name, value);

        public bool Remove(string name)
            => this._headers.Remove(name);

        public bool TryGet(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
            => this._headers.TryGetValues(name, out values);

        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
            => this._headers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();
    }
}

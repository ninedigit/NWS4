using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NineDigit.NWS4
{
    public class HttpRequestHeaders : Dictionary<string, IEnumerable<string>>, IHttpRequestHeaders
    {
        public HttpRequestHeaders()
            : base(StringComparer.OrdinalIgnoreCase)
        { }
        
        public HttpRequestHeaders(System.Net.Http.Headers.HttpRequestHeaders headers)
            : this(ToDictionary(headers))
        {
        }

        public HttpRequestHeaders(IDictionary<string, IEnumerable<string>> collection)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
                foreach (var value in item.Value)
                    this.Add(item.Key, value);
        }

        public void Add(string name, string? value)
        {
            value ??= string.Empty;
            
            if (TryGetValue(name, out var values))
            {
                var newValues = new List<string>(values) { value };
                this[name] = newValues;
            }
            else
            {
                this[name] = new List<string> { value };
            }
        }

        public bool TryGet(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
            => TryGetValue(name, out values);
        
        private static Dictionary<string, IEnumerable<string>> ToDictionary(System.Net.Http.Headers.HttpRequestHeaders self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));
            
            return self.ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
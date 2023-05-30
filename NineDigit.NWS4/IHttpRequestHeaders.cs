using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4
{
    public interface IReadOnlyHttpRequestHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
    {
        bool TryGet(string name, [NotNullWhen(true)] out IEnumerable<string>? values);
    }

    public interface IHttpRequestHeaders : IReadOnlyHttpRequestHeaders
    {
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value">Null values are allowed and will be normalized to empty strings.
        /// If the user adds multiple null/empty values, all of them are added to the collection.
        /// This will result in delimiter-only values, e.g. adding two null-strings (or empty,
        /// or whitespace-only) results in "My-Header: ,"</param>
        void Add(string name, string? value);
        bool Remove(string name);
    }
}

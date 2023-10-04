using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4;

public interface IReadOnlyHttpRequestHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
{
    bool TryGet(string name, [NotNullWhen(true)] out IEnumerable<string>? values);
}
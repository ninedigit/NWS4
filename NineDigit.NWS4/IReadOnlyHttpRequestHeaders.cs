using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NineDigit.NWS4;

public interface IReadOnlyHttpRequestHeaders : IEnumerable<KeyValuePair<string, string>>
{
    bool TryGet(string name, [NotNullWhen(true)] out string? value);
}
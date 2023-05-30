using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq
{
    internal static class DictionaryExtensions
    {
        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IDictionary<TKey, TValue> self) where TKey: notnull
            => new ReadOnlyDictionary<TKey, TValue>(self);

        public static KeyValuePair<TKey, TValue>? FirstOrNull<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> self, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            using var iterator = self.GetEnumerator();

            while (iterator.MoveNext())
            {
                var item = iterator.Current;
                var matches = predicate(item);

                if (matches)
                    return item;
            }

            return null;
        }
    }
}

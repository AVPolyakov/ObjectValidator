using System;
using System.Linq;

namespace ObjectValidator
{
    public static class MessageFormatter
    {
        public static string ReplacePlaceholderWithValue(this string seed, params Tuple<string, object>[] tuples)
            => tuples.Aggregate(seed, (current, tuple) => current.Replace($"{{{tuple.Item1}}}", tuple.Item2?.ToString()));

        public static Tuple<string, object> CreateTuple(string key, object value) => Tuple.Create(key, value);
    }
}
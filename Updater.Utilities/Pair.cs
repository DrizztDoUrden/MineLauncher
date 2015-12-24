using System.Collections.Generic;

namespace Updater.Utilities
{
    public class Pair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public Pair() { }

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public static Pair<TKey, TValue> FromKvp(KeyValuePair<TKey, TValue> kvp) => new Pair<TKey, TValue>(kvp.Key, kvp.Value);
    }
}

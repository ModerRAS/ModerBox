using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common {
    public static class DictionaryExtensions {
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second) {
            if (first == null) {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null) {
                throw new ArgumentNullException(nameof(second));
            }

            foreach (var pair in second) {
                first[pair.Key] = pair.Value;
            }

            return first;
        }
    }
}

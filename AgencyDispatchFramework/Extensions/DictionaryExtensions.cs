using System.Collections.Generic;

namespace AgencyDispatchFramework.Extensions
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds the key to the dictionary if it does not already contain the key, otherwise
        /// updates the value.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddOrUpdate<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, value);
            }
        }
    }
}

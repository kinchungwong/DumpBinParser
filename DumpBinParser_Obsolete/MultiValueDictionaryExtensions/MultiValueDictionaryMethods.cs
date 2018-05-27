using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.MultiValueDictionaryExtensions
{
    /// <summary>
    /// Extension methods for working with multi-value dictionary that is emulated 
    /// on top of a dictionary of lists.
    /// </summary>
    public static class MultiValueDictionaryMethods
    {
        public static Dictionary<TK, List<TV>> Create<TK, TV>()
        {
            return new Dictionary<TK, List<TV>>();
        }

        /// <summary>
        /// <para>
        /// For a dictionary which maps to a list of values, this extension method appends
        /// the specified value to the list associated with the specified key.
        /// </para>
        /// <para>
        /// If the key is not previously known to the dictionary, a new list is created
        /// and initialized with the specified value, and then the list is added to the
        /// dictionary associated with the specified key.
        /// </para>
        /// <para>
        /// This extension method is typically used to emulate a multi-value dictionary.
        /// </para>
        /// </summary>
        /// <typeparam name="TK">The dictionary's key type</typeparam>
        /// <typeparam name="TV">The dictionary's mapped element type.</typeparam>
        /// <param name="dict">The dictionary to be modified</param>
        /// <param name="k">The key</param>
        /// <param name="v">The value</param>
        /// <returns>True if the dictionary already contains the key. 
        /// False if the key is newly added.
        /// </returns>
        public static bool AppendValue<TK, TV>(this Dictionary<TK, List<TV>> dict, TK k, TV v)
        {
            bool exists = dict.TryGetValue(k, out List<TV> lv);
            if (exists)
            {
                lv.Add(v);
            }
            else
            {
                lv = new List<TV> { v };
                dict.Add(k, lv);
            }
            return exists;
        }

        /// <summary>
        /// <para>
        /// For a dictionary which maps to a list of values, this extension method appends
        /// the specified value to the list associated with the specified key.
        /// </para>
        /// <para>
        /// If the key is not previously known to the dictionary, a new list is created
        /// and initialized with the specified value, and then the list is added to the
        /// dictionary associated with the specified key.
        /// </para>
        /// <para>
        /// This extension method is typically used to emulate a multi-value dictionary.
        /// </para>
        /// </summary>
        /// <typeparam name="TK">The dictionary's key type</typeparam>
        /// <typeparam name="TV">The dictionary's mapped element type.</typeparam>
        /// <param name="dict">The dictionary to be modified</param>
        /// <param name="k">The key</param>
        /// <param name="v">The value</param>
        /// <returns>True if the dictionary already contains the key. 
        /// False if the key is newly added.
        /// </returns>
        public static bool AppendValue<TK, TV>(this Dictionary<TK, HashSet<TV>> dict, TK k, TV v)
        {
            bool exists = dict.TryGetValue(k, out HashSet<TV> lv);
            if (exists)
            {
                lv.Add(v);
            }
            else
            {
                lv = new HashSet<TV> { v };
                dict.Add(k, lv);
            }
            return exists;
        }

        /// <summary>
        /// <para>
        /// For a dictionary which maps to a list of values, this extension method appends
        /// the specified values to the list associated with the specified key.
        /// </para>
        /// <para>
        /// If the key is not previously known to the dictionary, a new list is created
        /// and initialized with the specified value, and then the list is added to the
        /// dictionary associated with the specified key.
        /// </para>
        /// <para>
        /// This extension method is typically used to emulate a multi-value dictionary.
        /// </para>
        /// </summary>
        /// <typeparam name="TK">The dictionary's key type</typeparam>
        /// <typeparam name="TV">The dictionary's mapped element type.</typeparam>
        /// <param name="dict">The dictionary to be modified</param>
        /// <param name="k">The key</param>
        /// <param name="vs">The list of values to be appended</param>
        /// <returns>True if the dictionary already contains the key. 
        /// False if the key is newly added.
        /// </returns>
        public static bool AppendValues<TK, TV>(this Dictionary<TK, List<TV>> dict, TK k, IEnumerable<TV> vs)
        {
            bool exists = dict.TryGetValue(k, out List<TV> lv);
            if (exists)
            {
                lv.AddRange(vs);
            }
            else
            {
                lv = new List<TV>(vs);
                dict.Add(k, lv);
            }
            return exists;
        }

        /// <summary>
        /// Converts the emulated multi-value dictionary into a read-only dictionary in which the 
        /// mapped list of values are also exposed in a read-only way.
        /// </summary>
        /// <typeparam name="TK">The dictionary's key type</typeparam>
        /// <typeparam name="TV">The dictionary's mapped element type.</typeparam>
        /// <param name="dict">The dictionary that has been populated that will be converted into a read-only one.</param>
        /// <returns>The read-only dictionary.</returns>
        public static IReadOnlyDictionary<TK, IReadOnlyList<TV>> ToReadOnly<TK, TV>(this Dictionary<TK, List<TV>> dict)
        {
            var temp = new Dictionary<TK, IReadOnlyList<TV>>();
            foreach (var kvp in dict)
            {
                temp.Add(kvp.Key, kvp.Value.AsReadOnly());
            }
            return new ReadOnlyDictionary<TK, IReadOnlyList<TV>>(temp);
        }
    }
}

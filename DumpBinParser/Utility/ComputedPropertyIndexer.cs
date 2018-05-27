using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.Utility
{
    /// <summary>
    /// <para>
    /// Given an object type (entity), and a function that either computes or extracts 
    /// a certain field from the object, this class maintains an index (search 
    /// accelerator) from that field to a list of mapped types.
    /// </para>
    /// </summary>
    /// <typeparam name="EntityType">
    /// The type of the full object (entity), typically consisting of multiple fields, 
    /// which might be stored or computed.
    /// </typeparam>
    /// <typeparam name="FieldType">
    /// The field type that is derived from the full object, to be used as the key for 
    /// indexing.
    /// </typeparam>
    /// <typeparam name="SurrogateKey">
    /// The element type for a list that can be looked up using the key of the field type.
    /// Each unique field value may correspond to zero, one, or more full objects 
    /// (entities). To allow retrieval of arbitrary number of items, a list of such 
    /// elements is stored.
    /// </typeparam>
    public class ComputedPropertyIndexer<EntityType, FieldType, SurrogateKey>
    {
        private readonly IDictionary<SurrogateKey, EntityType> _entityDict;

        private readonly Dictionary<FieldType, List<SurrogateKey>> _fieldDict =
            new Dictionary<FieldType, List<SurrogateKey>>();

        private readonly Func<EntityType, FieldType> _fieldExtractionFunc;

        private readonly Func<FieldType, bool> _fieldIgnoreFunc;

        /// <summary>
        /// Returns a read-only collection of unique field values.
        /// </summary>
        public IList<FieldType> UniqueFieldValues
        {
            get
            {
                FieldType[] keyArray = new FieldType[_fieldDict.Keys.Count];
                _fieldDict.Keys.CopyTo(keyArray, 0);
                return new ReadOnlyCollection<FieldType>(keyArray);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fieldExtractionFunc">
        /// A function that extracts or derives the field value given a full object value (entity).
        /// </param>
        /// <remarks>
        /// See also <see cref="ComputedPropertyIndexer(Func{T, TResult}, Func{T, TResult})"/>
        /// The default criterion for ignoring a field value is equality with the field type's 
        /// default value.
        /// </remarks>
        public ComputedPropertyIndexer(IDictionary<SurrogateKey, EntityType> entityDict, 
            Func<EntityType, FieldType> fieldExtractionFunc)
        {
            _entityDict = entityDict;
            _fieldExtractionFunc = fieldExtractionFunc;
            _fieldIgnoreFunc = (_) => EqualityComparer<FieldType>.Default.Equals(_, default);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fieldExtractionFunc">
        /// A function that extracts or derives the field value given a full object value (entity).
        /// </param>
        /// <param name="fieldIgnoreFunc">
        /// A function that determines whether a particular field value should be ignored 
        /// (not to be indexed). Typically, this function determines whether the partofilar 
        /// field is not applicable or missing on the given full object (entity).
        /// </param>
        public ComputedPropertyIndexer(IDictionary<SurrogateKey, EntityType> entityDict, 
            Func<EntityType, FieldType> fieldExtractionFunc, Func<FieldType, bool> fieldIgnoreFunc)
        {
            _entityDict = entityDict;
            _fieldExtractionFunc = fieldExtractionFunc;
            _fieldIgnoreFunc = fieldIgnoreFunc;
        }

        /// <summary>
        /// Adds a full object (entity) to the index.
        /// </summary>
        /// <param name="o">
        /// The full object (entity). This is the input to the field extraction function.
        /// </param>
        /// <param name="m">
        /// The mapped type (a key that can be used to look up the full object elsewhere).
        /// </param>
        public void Add(EntityType o, SurrogateKey m)
        {
            FieldType f = _fieldExtractionFunc(o);
            if (_fieldIgnoreFunc(f))
            {
                return;
            }
            if (!_fieldDict.TryGetValue(f, out List<SurrogateKey> ms))
            {
                ms = new List<SurrogateKey>();
                _fieldDict.Add(f, ms);
            }
            ms.Add(m);
        }

        public FieldType ExtractField(EntityType o)
        {
            return _fieldExtractionFunc(o);
        }

        /// <summary>
        /// Checks that 
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public bool Contains(FieldType f)
        {
            if (_fieldIgnoreFunc(f))
            {
                return false;
            }
            return _fieldDict.ContainsKey(f);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool TryFind(FieldType f, out List<SurrogateKey> ms)
        {
            ms = null;
            if (_fieldIgnoreFunc(f))
            {
                return false;
            }
            return _fieldDict.TryGetValue(f, out ms);
        }

        /// <summary>
        /// Given the field value, returns an IEnumerable that iterates through the full 
        /// objects (entities) stored in the dictionary.
        /// </summary>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        public IEnumerable<EntityType> this[FieldType fieldValue]
        {
            get
            {
                if (_fieldIgnoreFunc(fieldValue))
                {
                    yield break;
                }
                if (!_fieldDict.TryGetValue(fieldValue, out List<SurrogateKey> keys))
                {
                    yield break;
                }
                foreach (SurrogateKey key in keys)
                {
                    yield return _entityDict[key];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public SurrogateKey FirstOrDefault(FieldType f, SurrogateKey defaultValue = default)
        {
            if (_fieldIgnoreFunc(f))
            {
                return defaultValue;
            }
            if (!_fieldDict.TryGetValue(f, out var ms))
            {
                return defaultValue;
            }
            if (ms.Count == 0)
            {
                return defaultValue;
            }
            return ms[0];
        }
    }
}

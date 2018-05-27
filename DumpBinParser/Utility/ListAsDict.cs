using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.Utility
{
    public class ListAsDict<T> : IDictionary<int, T>
    {
        private List<T> _list;

        public List<T> UnderlyingList => _list;
        
        public ListAsDict(List<T> list)
        {
            _list = list;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public T this[int key]
        {
            get
            {
                return _list[key];
            }
            set
            {
                while (_list.Count <= key)
                {
                    _list.Add(default);
                }
                _list[key] = value;
            }
        }

        public ICollection<int> Keys
        {
            get
            {
                return (ICollection<int>)new Iota(Count);
            }
        }

        public ICollection<T> Values
        {
            get
            {
                return _list.AsReadOnly();
            }
        }

        IEnumerator<KeyValuePair<int, T>> IEnumerable<KeyValuePair<int, T>>.GetEnumerator()
        {
            for (int index = 0; index < _list.Count; ++index)
            {
                yield return new KeyValuePair<int, T>(index, _list[index]);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            for (int index = 0; index < _list.Count; ++index)
            {
                yield return new KeyValuePair<int, T>(index, _list[index]);
            }
        }

        public bool Remove(int index)
        {
            if (index < 0 || index >= _list.Count)
            {
                return false;
            }
            _list[index] = default;
            return true;
        }

        public bool Remove(KeyValuePair<int, T> kvp)
        {
            if (kvp.Key < 0 || kvp.Key >= _list.Count)
            {
                return false;
            }
            if (!EqualityComparer<T>.Default.Equals(_list[kvp.Key], kvp.Value))
            {
                return false;
            }
            _list[kvp.Key] = default;
            return true;
        }

        public void CopyTo(KeyValuePair<int, T>[] kvps, int arrayIndex)
        {
            for (int index = 0; index < _list.Count; ++index)
            {
                kvps[arrayIndex + index] = new KeyValuePair<int, T>(index, _list[index]);
            }
        }

        public bool Contains(KeyValuePair<int, T> kvp)
        {
            if (kvp.Key < 0 || kvp.Key >= _list.Count)
            {
                return false;
            }
            return EqualityComparer<T>.Default.Equals(_list[kvp.Key], kvp.Value);
        }

        public bool ContainsKey(int index)
        {
            return (index >= 0 && index < _list.Count);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public void Add(KeyValuePair<int, T> kvp)
        {
            Add(kvp.Key, kvp.Value);
        }

        public void Add(int key, T value)
        {
            this[key] = value;
        }

        public bool TryGetValue(int key, out T value)
        {
            value = default;
            if (key >= 0 && key < _list.Count)
            {
                value = _list[key];
                return !EqualityComparer<T>.Default.Equals(value, default);
            }
            return false;
        }

        public class Iota : IReadOnlyList<int>
        {
            private readonly int _count;

            public Iota(int count)
            {
                _count = count;
            }

            public int Count => _count;

            public bool ReadOnly => true;

            public int this[int index]
            {
                get
                {
                    if (index < 0 || index >= _count)
                    {
                        throw new IndexOutOfRangeException("index");
                    }
                    return index;
                }
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                for (int index = 0; index < _count; ++index)
                {
                    yield return index;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                for (int index = 0; index < _count; ++index)
                {
                    yield return index;
                }
            }
        }
    }
}

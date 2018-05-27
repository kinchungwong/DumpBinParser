using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.Utility
{
    public class UniqueQueue<T>
    {
        private HashSet<T> _hashSet = new HashSet<T>();
        private Queue<T> _queue = new Queue<T>();

        public void Enqueue(T value)
        {
            if (!_hashSet.Contains(value))
            {
                _hashSet.Add(value);
                _queue.Enqueue(value);
            }
        }

        public int Count
        {
            get
            {
                return _hashSet.Count;
            }
        }

        public T Dequeue()
        {
            T value = _queue.Dequeue();
            _hashSet.Remove(value);
            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class CallTable
    {
        private List<CallEntry> _entries;
        private Utility.ListAsDict<CallEntry> _entriesWrapper;
        private Utility.ComputedPropertyIndexer<CallEntry, CallEntry, int> _callEntryMatch;

        public CallTable()
        {
            _entries = new List<CallEntry>();
            _entriesWrapper = new Utility.ListAsDict<CallEntry>(_entries);
            _callEntryMatch = new Utility.ComputedPropertyIndexer<CallEntry, CallEntry, int>(_entriesWrapper, (_) => _);
        }

        public IList<CallEntry> CallEntries => _entries.AsReadOnly();

        public void Add(CallEntry entry)
        {
            if (_callEntryMatch.Contains(entry))
            {
                return;
            }
            int newId = _entries.Count;
            _entries.Add(entry);
            _callEntryMatch.Add(entry, newId);
        }

        public void AddRange(IEnumerable<CallEntry> entries)
        {
            foreach (var entry in entries)
            {
                Add(entry);
            }
        }
    }
}

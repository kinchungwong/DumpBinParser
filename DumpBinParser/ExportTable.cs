using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class ExportTable
    {
        private List<ExportEntry> _entries;
        private Utility.ListAsDict<ExportEntry> _entriesWrapper;
        private Utility.ComputedPropertyIndexer<ExportEntry, ExportEntry, int> _exactMatch;
        private Utility.ComputedPropertyIndexer<ExportEntry, string, int> _symbolIndex;
        private Utility.ComputedPropertyIndexer<ExportEntry, string, int> _shortNameIndex;
        private Utility.ComputedPropertyIndexer<ExportEntry, FileIdentity, int> _providerIndex;
        private Utility.ComputedPropertyIndexer<ExportEntry, string, int> _prototypeIndex;

        public ExportTable()
        {
            _entries = new List<ExportEntry>();
            _entriesWrapper = new Utility.ListAsDict<ExportEntry>(_entries);
            _exactMatch = new Utility.ComputedPropertyIndexer<ExportEntry, ExportEntry, int>(_entriesWrapper, (_) => _);
            _symbolIndex = new Utility.ComputedPropertyIndexer<ExportEntry, string, int>(_entriesWrapper, (_) => _.Symbol);
            _shortNameIndex = new Utility.ComputedPropertyIndexer<ExportEntry, string, int>(_entriesWrapper, (_) => _.ShortFunctionName);
            _providerIndex = new Utility.ComputedPropertyIndexer<ExportEntry, FileIdentity, int>(_entriesWrapper, (_) => _.ProviderIdentity);
            _prototypeIndex = new Utility.ComputedPropertyIndexer<ExportEntry, string, int>(_entriesWrapper, (_) => _.FunctionPrototype);
        }

        public IList<ExportEntry> Entries
        {
            get
            {
                return _entries.AsReadOnly();
            }
        }

        public IList<string> Symbols
        {
            get
            {
                return _symbolIndex.UniqueFieldValues;
            }
        }

        public IList<string> ShortFunctionNames
        {
            get
            {
                return _shortNameIndex.UniqueFieldValues;
            }
        }

        public IList<FileIdentity> Providers
        {
            get
            {
                return _providerIndex.UniqueFieldValues;
            }
        }

        public void Add(ExportEntry entry)
        {
            if (_exactMatch.Contains(entry))
            {
                return;
            }
            // Do not rearrange "newId" and "_entries.Add()"
            int newId = _entries.Count;
            _entries.Add(entry);
            _exactMatch.Add(entry, newId);
            _symbolIndex.Add(entry, newId);
            _shortNameIndex.Add(entry, newId);
            _providerIndex.Add(entry, newId);
            _prototypeIndex.Add(entry, newId);
        }

        public void AddRange(IList<ExportEntry> entries)
        {
            foreach (var entry in entries)
            {
                Add(entry);
            }
        }

        public List<ExportEntry> EntriesForSymbol(string symbol)
        {
            var entries = new List<ExportEntry>();
            if (!_symbolIndex.TryFind(symbol, out var ids))
            {
                return entries;
            }
            foreach (int id in ids)
            {
                entries.Add(_entries[id]);
            }
            return entries;
        }

        public List<ExportEntry> EntriesForProvider(FilePath provider)
        {
            return EntriesForProvider(provider.Identity);
        }

        public List<ExportEntry> EntriesForProvider(FileIdentity providerIdentity)
        {
            var entries = new List<ExportEntry>();
            if (!_providerIndex.TryFind(providerIdentity, out var ids))
            {
                return entries;
            }
            foreach (int id in ids)
            {
                entries.Add(_entries[id]);
            }
            return entries;
        }
    }
}

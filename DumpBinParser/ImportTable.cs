using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DumpBinParser.Utility;

namespace DumpBinParser
{
    /// <summary>
    /// All function import declarations.
    /// </summary>
    /// <remarks>
    /// This table class provides single-column indexing for each column of this table.
    /// This allows querying the list of entries using one or more exact-matching column values.
    /// </remarks>
    public class ImportTable
    {
        private List<ImportEntry> _entries;
        private Utility.ListAsDict<ImportEntry> _entriesWrapper;
        private Utility.ComputedPropertyIndexer<ImportEntry, ImportEntry, int> _exactMatch;
        private Utility.ComputedPropertyIndexer<ImportEntry, string, int> _symbolIndex;
        private Utility.ComputedPropertyIndexer<ImportEntry, string, int> _shortNameIndex;
        private Utility.ComputedPropertyIndexer<ImportEntry, FileIdentity, int> _callerIndex;
        private Utility.ComputedPropertyIndexer<ImportEntry, FileHint, int> _providerIndex;

        public ImportTable()
        {
            _entries = new List<ImportEntry>();
            _entriesWrapper = new Utility.ListAsDict<ImportEntry>(_entries);
            _exactMatch = new Utility.ComputedPropertyIndexer<ImportEntry, ImportEntry, int>(_entriesWrapper, (_) => _);
            _symbolIndex = new Utility.ComputedPropertyIndexer<ImportEntry, string, int>(_entriesWrapper, (_) => _.Symbol);
            _shortNameIndex = new Utility.ComputedPropertyIndexer<ImportEntry, string, int>(_entriesWrapper, (_) => _.ShortFunctionName);
            _callerIndex = new Utility.ComputedPropertyIndexer<ImportEntry, FileIdentity, int>(_entriesWrapper, (_) => _.CalledFromFileIdentity);
            _providerIndex = new Utility.ComputedPropertyIndexer<ImportEntry, FileHint, int>(_entriesWrapper, (_) => _.ProviderFileHint);
        }

        public IList<ImportEntry> Entries
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

        public IList<FileIdentity> CallerFileIdentities
        {
            get
            {
                return _callerIndex.UniqueFieldValues;
            }
        }

        public IList<FileHint> Providers
        {
            get
            {
                return _providerIndex.UniqueFieldValues;
            }
        }

        public void Add(ImportEntry importEntry)
        {
            if (_exactMatch.Contains(importEntry))
            {
                return;
            }
            // Do not rearrange "newId" and "_entries.Add()"
            int newId = _entries.Count;
            _entries.Add(importEntry);
            _exactMatch.Add(importEntry, newId);
            _symbolIndex.Add(importEntry, newId);
            _shortNameIndex.Add(importEntry, newId);
            _callerIndex.Add(importEntry, newId);
            _providerIndex.Add(importEntry, newId);
        }

        public void AddRange(IList<ImportEntry> entries)
        {
            foreach (var importEntry in entries)
            {
                Add(importEntry);
            }
        }

        public List<ImportEntry> EntriesForSymbol(string symbol)
        {
            var entries = new List<ImportEntry>();
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

        public List<ImportEntry> EntriesForCaller(FilePath caller)
        {
            return EntriesForCaller(caller.Identity);
        }

        public List<ImportEntry> EntriesForCaller(FileIdentity caller)
        {
            var entries = new List<ImportEntry>();
            if (!_callerIndex.TryFind(caller, out var ids))
            {
                return entries;
            }
            foreach (int id in ids)
            {
                entries.Add(_entries[id]);
            }
            return entries;
        }

        public List<ImportEntry> EntriesForProvider(FilePath provider)
        {
            return EntriesForProvider(provider.FileHint);
        }

        public List<ImportEntry> EntriesForProvider(FileHint providerHint)
        {
            var entries = new List<ImportEntry>();
            if (!_providerIndex.TryFind(providerHint, out var ids))
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

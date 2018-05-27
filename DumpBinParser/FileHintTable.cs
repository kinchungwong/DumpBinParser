using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class FileHintTable
    {
        private List<FileHint> _fileHints;
        private Utility.ListAsDict<FileHint> _fileHintsWrapper;
        private Utility.ComputedPropertyIndexer<FileHint, string, int> _fileHintMatch;

        public FileHintTable()
        {
            _fileHints = new List<FileHint>();
            _fileHintsWrapper = new Utility.ListAsDict<FileHint>(_fileHints);
            _fileHintMatch = new Utility.ComputedPropertyIndexer<FileHint, string, int>(
                _fileHintsWrapper, (_) => _.PathlessFileName.ToLowerInvariant());
        }

        public IReadOnlyList<FileHint> FileHints => _fileHints.AsReadOnly();

        public int Add(FileHint fileHint)
        {
            var field = _fileHintMatch.ExtractField(fileHint);
            if (_fileHintMatch.Contains(field))
            {
                return _fileHintMatch.FirstOrDefault(field);
            }
            int newHintId = _fileHints.Count;
            _fileHints.Add(fileHint);
            _fileHintMatch.Add(fileHint, newHintId);
            return newHintId;
        }
    }
}

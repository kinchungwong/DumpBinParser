using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class FilePathTable
    {
        private List<FilePath> _filePaths;
        private Utility.ListAsDict<FilePath> _filePathsWrapper;
        private Utility.ComputedPropertyIndexer<FilePath, FilePath, int> _filePathMatch;
        private Utility.ComputedPropertyIndexer<FilePath, FileHint, int> _fileHintMatch;
        private Utility.ComputedPropertyIndexer<FilePath, string, int> _fullNameIndex;
        private Utility.ComputedPropertyIndexer<FilePath, string, int> _checksumIndex;

        public FilePathTable()
        {
            _filePaths = new List<FilePath>();
            _filePathsWrapper = new Utility.ListAsDict<FilePath>(_filePaths);
            _filePathMatch = new Utility.ComputedPropertyIndexer<FilePath, FilePath, int>(_filePathsWrapper, (_) => _);
            _fileHintMatch = new Utility.ComputedPropertyIndexer<FilePath, FileHint, int>(_filePathsWrapper, (_) => _.FileHint);
            _fullNameIndex = new Utility.ComputedPropertyIndexer<FilePath, string, int>(_filePathsWrapper, (_) => _.FullName);
            _checksumIndex = new Utility.ComputedPropertyIndexer<FilePath, string, int>(_filePathsWrapper, (_) => _.Identity.Checksum);
        }

        public IReadOnlyList<FilePath> FilePaths => _filePaths.AsReadOnly();
        //public IReadOnlyList<FileHint> FileHints => _fileHints.AsReadOnly();

        public int Add(FilePath filePath)
        {
            if (_filePathMatch.Contains(filePath))
            {
                return _filePathMatch.FirstOrDefault(filePath);
            }
            int newPathId = _filePaths.Count;
            _filePaths.Add(filePath);
            _filePathMatch.Add(filePath, newPathId);
            _fileHintMatch.Add(filePath, newPathId);
            _fullNameIndex.Add(filePath, newPathId);
            _checksumIndex.Add(filePath, newPathId);
            return newPathId;
        }

        public IEnumerable<FilePath> FilePathsForChecksum(string checksum)
        {
            if (!_checksumIndex.TryFind(checksum, out List<int> ids))
            {
                yield break;
            }
            foreach (int id in ids)
            {
                yield return _filePaths[id];
            }
        }
    }
}

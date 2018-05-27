using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    /// <summary>
    /// <para>
    /// A file hint is one or more pieces of information that may help locate
    /// a dependent file. The actual file may or may not exist.
    /// </para>
    /// </summary>
    public class FileHint : IEquatable<FileHint>
    {
        public string PathlessFileName
        {
            get;
            private set;
        }

        public FileHint(string pathlessFileName)
        {
            PathlessFileName = pathlessFileName;
        }

        public override string ToString()
        {
            return PathlessFileName;
        }

        public override int GetHashCode()
        {
            return PathlessFileName.GetHashCode();
        }

        public bool Equals(FileHint other)
        {
            if (other == null)
            {
                return false;
            }
            return PathlessFileName.Equals(other.PathlessFileName);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class FilePath : IEquatable<FilePath>
    {
        public string FullName
        {
            get;
            private set;
        }

        public FileIdentity Identity
        {
            get;
            private set;
        }

        public FileHint FileHint
        {
            get
            {
                return new FileHint(System.IO.Path.GetFileName(FullName));
            }
        }

        public FilePath(string fullName)
        {
            FullName = fullName;
            Identity = FileIdentity.ComputeFromFile(fullName);
        }

        public FilePath(string fullName, FileIdentity identity)
        {
            FullName = fullName;
            Identity = identity;
        }

        public bool Equals(FilePath other)
        {
            if (other == null)
            {
                return false;
            }
            return FullName.Equals(other.FullName) &&
                Identity.Equals(other.Identity);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FilePath);
        }

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        public override string ToString()
        {
            return FullName + "(" + Identity.ToString() + ")";
        }
    }
}

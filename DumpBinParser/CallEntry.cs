using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class CallEntry : IEquatable<CallEntry>
    {
        public FilePath CalledFromFile
        {
            get;
            private set;
        }

        public FileIdentity CalledFromFileIdentity
        {
            get
            {
                return CalledFromFile.Identity;
            }
        }

        public string CallerSymbol
        {
            get;
            private set;
        }

        public string CalleeSymbol
        {
            get;
            private set;
        }

        public CallEntry(FilePath calledFromFile, string callerSymbol, string calleeSymbol)
        {
            CalledFromFile = calledFromFile;
            CallerSymbol = callerSymbol;
            CalleeSymbol = calleeSymbol;
        }

        public override string ToString()
        {
            return CalleeSymbol + "(" + CallerSymbol + ")";
        }

        public bool Equals(CallEntry other)
        {
            if (other == null)
            {
                return false;
            }
            return CalledFromFile.Identity.Equals(other.CalledFromFile.Identity) &&
                CallerSymbol.Equals(other.CallerSymbol) &&
                CalleeSymbol.Equals(other.CalleeSymbol);
        }

        public override int GetHashCode()
        {
            return CallerSymbol.GetHashCode() ^ CalleeSymbol.GetHashCode();
        }
    }
}

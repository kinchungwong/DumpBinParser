using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.DumpBin
{
    using Utility;

    /// <summary>
    /// A data class representing one line of output from "dumpbin.exe" 
    /// using the "EXPORTS" option.
    /// </summary>
    public class DumpBinExportsRecord
    {
        public int Ordinal { get; set; }
        public int Hint { get; set; }
        public uint RVA { get; set; }
        public string Name { get; set; }
        public string DecoratedName { get; set; }
        public string Prototype { get; set; }

        public BalancedBracketParser TryParse()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return null;
            }
            var bbp = new BalancedBracketParser(Name);
            if (bbp.BracketCount >= 1)
            {
                Prototype = bbp.GetBracketContent(0);
            }
            int equalSignIndex = Name.IndexOf('=');
            if (equalSignIndex >= 0)
            {
                DecoratedName = Name.Substring(0, equalSignIndex).Trim();
            }
            return bbp;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            // TODO
            return Name.GetHashCode();
        }
    }
}

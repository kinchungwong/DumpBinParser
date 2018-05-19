using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser.DumpBin
{
    /// <summary>
    /// A data class representing observed function calls (those that are not 
    /// being inlined) parsed from the disassembly output of "dumpbin.exe" 
    /// using the "DISASM" option.
    /// </summary>
    public class DumpBinCallRecord
    {
        public string Caller { get; set; }
        public string Callee { get; set; }
        public ulong RVA { get; set; }

        public override string ToString()
        {
            return Caller + "+" + RVA.ToString("X") + ":" + Callee;
        }

        public override int GetHashCode()
        {
            // TODO
            return ToString().GetHashCode();
        }
    }
}

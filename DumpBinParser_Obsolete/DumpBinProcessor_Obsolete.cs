using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    using DumpBin;

    public static class DumpBinProcessor_Obsolete
    {
        [Obsolete("Use GetExports() => List<ExportEntry> instead")]
        public static List<DumpBinExportsRecord> GetExports_Obsolete(string inputFilename)
        {
            if (!File.Exists(inputFilename))
            {
                throw new FileNotFoundException(inputFilename);
            }
            var invoker = new DumpBinInvoker();
            invoker.Arguments.Add("/EXPORTS");
            invoker.Arguments.Add(inputFilename);
            invoker.Run();
            char[] splitChars = new char[] { ' ', '\t' };
            bool hasStarted = false;
            var records = new List<DumpBinExportsRecord>();
            foreach (string line in invoker.Outputs)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }
                if (!hasStarted)
                {
                    if (trimmed.Contains("ordinal") && trimmed.Contains("hint") && trimmed.Contains("RVA") && trimmed.Contains("name"))
                    {
                        hasStarted = true;
                    }
                    continue;
                }
                if (trimmed.Equals("Summary"))
                {
                    break;
                }
                string[] parts = trimmed.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                {
                    throw new Exception("Unrecognized text in dumpbin.exe EXPORTS output: \n    " + trimmed + "\n");
                }
                string sOrdinal = parts[0];
                string sHint = parts[1];
                string sRVA = parts[2];
                int nameStart = trimmed.IndexOf(sRVA);
                string name = trimmed.Substring(nameStart + sRVA.Length).Trim();
                DumpBinExportsRecord record = new DumpBinExportsRecord()
                {
                    Ordinal = int.Parse(sOrdinal),
                    Hint = int.Parse(sHint, System.Globalization.NumberStyles.AllowHexSpecifier),
                    RVA = uint.Parse(sRVA, System.Globalization.NumberStyles.AllowHexSpecifier),
                    Name = name
                };
                record.TryParse();
                records.Add(record);
            }
            return records;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace DumpBinParser.DumpBin
{
    /// <summary>
    /// Invokes "dumpbin.exe" with "EXPORTS" option on the specified binary file.
    /// </summary>
    public class DumpBinExportsInvoker : DumpBinInvokerBase
    {
        /// <summary>
        /// Path the the binary file to be analyzed by "dumpbin.exe" 
        /// </summary>
        public string InputBinaryPath
        {
            get;
        }

        /// <summary>
        /// All output text from "dumpbin.exe"
        /// </summary>
        public List<string> OutputLines
        {
            get;
        } = new List<string>();

        public List<DumpBinExportsRecord> Records
        {
            get;
        } = new List<DumpBinExportsRecord>();

        /// <summary>
        /// Creates a DumpBinExportsInvoker with the specified input binary file.
        /// </summary>
        /// <param name="inputBinaryFilename">
        /// The binary file to be analyzed with "dumpbin.exe"
        /// </param>
        public DumpBinExportsInvoker(string inputBinaryFilename)
        {
            InputBinaryPath = inputBinaryFilename;
            if (!File.Exists(InputBinaryPath))
            {
                throw new FileNotFoundException("Input binary file not found", InputBinaryPath);
            }
        }

        /// <summary>
        /// Run the "dumpbin.exe" utility on the input binary file.
        /// </summary>
        public void Run()
        {
            EnsureExePathSet();
            RunProcess();
            ParseOutput();
        }

        private void RunProcess()
        {
            string args = "/EXPORTS " + EnsurePathQuoted(InputBinaryPath);
            ProcessStartInfo psi = new ProcessStartInfo(ExePath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            using (var process = Process.Start(psi))
            using (var srt = new Utility.StreamReaderThread(process.StandardOutput))
            {
                process.WaitForExit();
                srt.WaitForExit();
                while (srt.Lines.TryDequeue(out string s))
                {
                    OutputLines.Add(s);
                }
            }
        }

        private void ParseOutput()
        {
            char[] splitChars = new char[] { ' ', '\t' };
            bool hasStarted = false;
            foreach (string s in OutputLines)
            {
                if (!hasStarted)
                {
                    if (s.Contains("ordinal") && s.Contains("hint") && s.Contains("RVA") && s.Contains("name"))
                    {
                        hasStarted = true;
                    }
                    continue;
                }
                string[] parts = s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }
                if (parts.Length == 1 && 
                    parts[0].ToLowerInvariant().Equals("summary", StringComparison.Ordinal))
                {
                    break;
                }
                if (parts.Length < 4)
                {
                    throw new Exception("Unrecognized text in dumpbin.exe EXPORTS output: \n" + s + "\n");
                }
                string sOrdinal = parts[0];
                string sHint = parts[1];
                string sRVA = parts[2];
                int nameStart = s.IndexOf(sRVA);
                string name = s.Substring(nameStart + sRVA.Length).Trim();
                DumpBinExportsRecord record = new DumpBinExportsRecord()
                {
                    Ordinal = int.Parse(sOrdinal),
                    Hint = int.Parse(sHint, System.Globalization.NumberStyles.AllowHexSpecifier),
                    RVA = uint.Parse(sRVA, System.Globalization.NumberStyles.AllowHexSpecifier),
                    Name = name
                };
                record.TryParse();
                Records.Add(record);
            }
        }
    }
}

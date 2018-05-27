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
    [Obsolete("only used by DumpBinImportsInvoker, which is also obsolete.")]
    public class ImportsIntermedList
    {
        public string ImportIntoFilename
        {
            get;
            set;
        }

        public int OutputLineIndex
        {
            get;
            set;
        }

        public string ImportFromFilename
        {
            get;
            set;
        }

        public Dictionary<string, string> AdditionalInfos
        {
            get;
        } = new Dictionary<string, string>();

        public Dictionary<string, uint> ImportedFunctions
        {
            get;
        } = new Dictionary<string, uint>();

        public ImportsIntermedList(string importIntoFilename, string importFromFilename, int outputLineIndex)
        {
            ImportIntoFilename = importIntoFilename;
            ImportFromFilename = importFromFilename;
            OutputLineIndex = outputLineIndex;
        }
    }

    public class DumpBinImportsInvoker : DumpBinInvokerBase
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
        public IList<string> OutputLines
        {
            get
            {
                return Invoker.OutputText;
            }
        }

        /// <summary>
        /// List of functions said to be imported from particular binaries.
        /// </summary>
        /// <remarks>
        /// The names of binaries do not contain full path.
        /// </remarks>
        public List<DumpBinImportsRecord> Records
        {
            get;
        } = new List<DumpBinImportsRecord>();

        /// <summary>
        /// <para>
        /// A list of callee function names generated from the imports. 
        /// </para>
        /// <para>
        /// Once generated, this list can be optionally provided to the disassembly parser. This helps 
        /// parsing because the parsing of function names from disassembly is not foolproof. A function 
        /// name that is not seen elsewhere may be disregarded.
        /// </para>
        /// </summary>
        public Dictionary<string, string> CalleeMapper
        {
            get;
            set;
        } = new Dictionary<string, string>();

        /// <summary>
        /// All parse errors (terminating and non-terminating).
        /// </summary>
        public List<Exception> ParseErrors
        {
            get;
        } = new List<Exception>();

        /// <summary>
        /// Creates a DumpBinExportsInvoker with the specified input binary file.
        /// </summary>
        /// <param name="inputBinaryFilename">
        /// The binary file to be analyzed with "dumpbin.exe"
        /// </param>
        public DumpBinImportsInvoker(string inputBinaryFilename)
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
            ParseIntermedList();
        }

        private void RunProcess()
        {
            using (Invoker = new Utility.ProcessInvoker())
            {
                Invoker.ExePath = ExePath;
                Invoker.Arguments.Add("/IMPORTS");
                Invoker.Arguments.Add(InputBinaryPath);
                Invoker.Run();
            }
        }

        private List<ImportsIntermedList> _intermedList = new List<ImportsIntermedList>();

        private void ParseOutput()
        {
            char[] splitChars = new char[] { ' ', '\t' };
            int lineCount = OutputLines.Count;
            bool hasStarted = false;
            ImportsIntermedList currentInfo = null;
            for (int lineIndex = 0; lineIndex < lineCount; ++lineIndex)
            {
                string line = OutputLines[lineIndex];
                if (!hasStarted)
                {
                    if (line.Trim().Equals("Section contains the following imports:", StringComparison.InvariantCulture))
                    {
                        hasStarted = true;
                    }
                    continue;
                }
                else
                {
                    if (line.Trim().Equals("Summary", StringComparison.InvariantCulture))
                    {
                        return;
                    }
                }
                string[] parts = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }
                if (parts.Length == 1)
                {
                    currentInfo = new ImportsIntermedList(InputBinaryPath, parts[0], lineIndex);
                    _intermedList.Add(currentInfo);
                    continue;
                }
                else if (parts.Length == 2)
                {
                    if (!IsHexString(parts[0]))
                    {
                        ParseErrors.Add(new Exception("Expects imported function to start with hexadecimal value. Text: \n" + line + "\n"));
                        continue;
                    }
                    string strValue = parts[0];
                    string remainder = line.Substring(line.IndexOf(strValue) + strValue.Length).Trim();
                    currentInfo.ImportedFunctions.Add(remainder, uint.Parse(strValue, System.Globalization.NumberStyles.AllowHexSpecifier));
                }
                else if (parts.Length > 2)
                {
                    if (!IsHexString(parts[0]))
                    {
                        ParseErrors.Add(new Exception("Expects additional information to start with hexadecimal value. Text: \n" + line + "\n"));
                        continue;
                    }
                    string strValue = parts[0];
                    string remainder = line.Substring(line.IndexOf(strValue) + strValue.Length).Trim();
                    currentInfo.AdditionalInfos.Add(remainder, strValue);
                }
            }
        }

        private void ParseIntermedList()
        {
            foreach (ImportsIntermedList intermed in _intermedList)
            {
                foreach (var functionNameAndValue in intermed.ImportedFunctions)
                {
                    string functionName = functionNameAndValue.Key;
                    var record = new DumpBinImportsRecord()
                    {
                        ProviderBinaryFilename = intermed.ImportFromFilename,
                        FunctionName = functionName
                    };
                    Records.Add(record);
                    if (!CalleeMapper.ContainsKey(functionName))
                    {
                        CalleeMapper.Add(functionName, functionName);
                    }
                }
            }
        }

        private bool IsHexString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException("Input should contain one or more non-whitespace characters.");
            }
            foreach (char c in s.Trim())
            {
                if (!IsHexChar(c))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}

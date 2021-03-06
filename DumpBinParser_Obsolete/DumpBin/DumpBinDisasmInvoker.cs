﻿using System;
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
    /// Invokes "dumpbin.exe" with "DISASM" option on the specified binary file.
    /// </summary>
    public class DumpBinDisasmInvoker : DumpBinInvokerBase
    {
        /// <summary>
        /// Path the the binary file to be analyzed by "dumpbin.exe" with the "DISASM" option.
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
        /// <para>
        /// A list of callee function names that can be optionally provided to 
        /// disassembly parser.
        /// </para>
        /// <para>
        /// This helps parsing because the parsing of function names from disassembly
        /// is not foolproof. A function name that is not seen elsewhere may be 
        /// disregarded.
        /// </para>
        /// </summary>
        public Dictionary<string, string> CalleeMapper
        {
            get;
            set;
        } = new Dictionary<string, string>();

        /// <summary>
        /// List of function calls (those not inlined) observed in disassembly.
        /// </summary>
        public List<DumpBinCallRecord> CallRecords
        {
            get;
        } = new List<DumpBinCallRecord>();

        /// <summary>
        /// Creates a DumpBinExportsInvoker with the specified input binary file.
        /// </summary>
        /// <param name="inputBinaryFilename">
        /// The binary file to be analyzed with "dumpbin.exe"
        /// </param>
        public DumpBinDisasmInvoker(string inputBinaryFilename)
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
            ParseHeader();
            ParseFunctionNames();
            ParseFunctionCalls();
        }

        private void RunProcess()
        {
            using (Invoker = new Utility.ProcessInvoker())
            {
                Invoker.ExePath = ExePath;
                Invoker.Arguments.Add("/DISASM");
                Invoker.Arguments.Add(InputBinaryPath);
                Invoker.Run();
            }
        }

        private int _firstLineAfterHeader;

        private void ParseHeader()
        {
            int headerIndicator = 0;
            int nextLineIndex = 0;
            foreach (string s in OutputLines)
            {
                int currLineIndex = (nextLineIndex++);
                if (headerIndicator < 2)
                {
                    if (s.ToLowerInvariant().StartsWith("dump of file"))
                    {
                        headerIndicator++;
                    }
                    else if (s.ToLowerInvariant().StartsWith("file type:"))
                    {
                        headerIndicator++;
                    }
                    continue;
                }
                if (headerIndicator == 2)
                {
                    _firstLineAfterHeader = currLineIndex;
                    return;
                }
            }
            throw new Exception("Unable to parse through lines of header from output.");
        }

        SortedSet<int> _funcLineIndex = new SortedSet<int>();

        /// <summary>
        /// Two-phase parsing is necessary because the list of function names
        /// are needed as the candidate set in the string matching of the 
        /// disassembled code.
        /// </summary>
        private void ParseFunctionNames()
        {
            int nextLineIndex = 0;
            foreach (string s in OutputLines)
            {
                int currLineIndex = (nextLineIndex++);
                if (currLineIndex < _firstLineAfterHeader)
                {
                    continue;
                }
                if (s.EndsWith(":"))
                {
                    string funcName = s.Substring(0, s.Length - 1);
                    _funcLineIndex.Add(currLineIndex);
                    if (!CalleeMapper.ContainsKey(funcName))
                    {
                        CalleeMapper.Add(funcName, funcName);
                    }
                }
            }
        }

        private void ParseFunctionCalls()
        {
            char[] spaceChars = new char[] { ' ', '\t' };
            char[] splitChars = new char[] { ' ', ',', '[', '+', ']', '-' };
            int currFuncLineIndex = -1;
            string currFuncName = null;
            int nextLineIndex = 0;
            ulong rva = ulong.MaxValue;
            foreach (string s in OutputLines)
            {
                int currLineIndex = (nextLineIndex++);
                if (currLineIndex < _firstLineAfterHeader)
                {
                    continue;
                }
                if (_funcLineIndex.Contains(currLineIndex))
                {
                    currFuncLineIndex = currLineIndex;
                    currFuncName = s.Substring(0, s.Length - 1);
                    continue;
                }
                if (currFuncLineIndex < 0 || string.IsNullOrEmpty(currFuncName))
                {
                    continue;
                }
                string[] parts = s.Split(spaceChars, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    if (IsRVA(part))
                    {
                        rva = ulong.Parse(part.TrimEnd(':'), System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    else if (IsDisasmByte(part))
                    {
                        // do nothing
                    }
                    else
                    {
                        string[] codeParts = part.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string codePart in codeParts)
                        {
                            if (IsDecimalChar(codePart[0]))
                            {
                                continue;
                            }
                            if (CalleeMapper.TryGetValue(codePart, out string callee))
                            {
                                var record = new DumpBinCallRecord()
                                {
                                    Caller = currFuncName,
                                    Callee = callee,
                                    RVA = rva
                                };
                                CallRecords.Add(record);
                            }
                        }
                    }
                }
            }
        }

        private bool IsRVA(string s)
        {
            if (s.Length < 8)
            {
                return false;
            }
            int hexLength = 0;
            foreach (char c in s)
            {
                if (c == ':')
                {
                    continue;
                }
                if (IsHexChar(c))
                {
                    hexLength++;
                }
                else
                {
                    return false;
                }
            }
            return (hexLength >= 8);
        }

        private bool IsDisasmByte(string s)
        {
            return (s.Length == 2 && IsHexChar(s[0]) && IsHexChar(s[1]));
        }

        private bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        private bool IsDecimalChar(char c)
        {
            return (c >= '0' && c <= '9');
        }
    }
}

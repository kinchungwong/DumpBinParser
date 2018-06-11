using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    using MultiValueDictionaryExtensions;

    public static class DumpBinProcessor
    {
        public static List<string> GetDependents(string inputFilename)
        {
            if (!File.Exists(inputFilename))
            {
                throw new FileNotFoundException(inputFilename);
            }
            var results = new List<string>();
            var invoker = new DumpBinInvoker();
            invoker.Arguments.Add("/DEPENDENTS");
            invoker.Arguments.Add(inputFilename);
            invoker.Run();
            bool hasStarted = false;
            foreach (string line in invoker.Outputs)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }
                if (!hasStarted)
                {
                    if (trimmed.Equals("Image has the following dependencies:", StringComparison.Ordinal))
                    {
                        hasStarted = true;
                    }
                    continue;
                }
                if (trimmed.Equals("Summary"))
                {
                    break;
                }
                if (trimmed.Equals("Image has the following delay load dependencies:", StringComparison.Ordinal))
                {
                    break;
                }
                results.Add(trimmed);
            }
            return results;
        }

        private static bool TryParseExportEntry(string outputLine, out string prototype, out string decoratedName)
        {
            prototype = null;
            decoratedName = null;
            if (string.IsNullOrEmpty(outputLine))
            {
                return false;
            }
            var bbp = new Utility.BalancedBracketParser(outputLine);
            if (bbp.BracketCount >= 1)
            {
                prototype = bbp.GetBracketContent(0);
            }
            int equalSignIndex = outputLine.IndexOf('=');
            if (equalSignIndex >= 0)
            {
                decoratedName = outputLine.Substring(0, equalSignIndex).Trim();
            }
            return !string.IsNullOrEmpty(prototype) ||
                !string.IsNullOrEmpty(decoratedName);
        }

        public static List<ExportEntry> GetExports(FilePath targetFile)
        {
            var invoker = new DumpBinInvoker();
            invoker.Arguments.Add("/EXPORTS");
            invoker.Arguments.Add(targetFile.FullName);
            invoker.Run();
            char[] splitChars = new char[] { ' ', '\t' };
            bool hasStarted = false;
            var exports = new List<ExportEntry>();
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
                if (!IsDecimalString(parts[0]) ||
                    !IsHexString(parts[1]) ||
                    !IsHexString(parts[2]))
                {
                    throw new Exception("Unrecognized text in dumpbin.exe EXPORTS output: \n    " + trimmed + "\n");
                }
#if false
                // Additional information skipped.
                string sOrdinal = parts[0];
                string sHint = parts[1];
#endif
                string sRVA = parts[2];
                int nameStart = trimmed.IndexOf(sRVA);
                string name = trimmed.Substring(nameStart + sRVA.Length).Trim();
                if (!TryParseExportEntry(name, out string prototype, out string decoratedName))
                {
                    throw new Exception("Unrecognized text in dumpbin.exe EXPORTS output: \n    " + trimmed + "\n");
                }
                exports.Add(new ExportEntry(targetFile, prototype, decoratedName, decoratedName));
            }
            return exports;
        }

        public static List<ImportEntry> GetImports(FilePath targetFile)
        {
            var invoker = new DumpBinInvoker();
            invoker.Arguments.Add("/IMPORTS");
            invoker.Arguments.Add(targetFile.FullName);
            invoker.Run();
            char[] splitChars = new char[] { ' ', '\t' };
            int lineCount = invoker.Outputs.Count;
            bool hasStarted = false;
            var imports = new List<ImportEntry>();
            FileHint calleeFileHint = null;
            for (int lineIndex = 0; lineIndex < lineCount; ++lineIndex)
            {
                string line = invoker.Outputs[lineIndex];
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
                        break;
                    }
                }
                string[] parts = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }
                if (parts.Length == 1)
                {
                    calleeFileHint = new FileHint(parts[0]);
                    continue;
                }
                else if (parts.Length == 2)
                {
                    if (!IsHexString(parts[0]))
                    {
                        throw new Exception("Expects imported function to start with hexadecimal value. Text: \n" + line + "\n");
                    }
                    if (calleeFileHint == null)
                    {
                        throw new Exception("Invalid execution flow: missing file name");
                    }
                    string strValue = parts[0];
                    string remainder = line.Substring(line.IndexOf(strValue) + strValue.Length).Trim();
                    imports.Add(new ImportEntry(targetFile, calleeFileHint, remainder, remainder));
                }
                else if (parts.Length > 2)
                {
                    if (!IsHexString(parts[0]))
                    {
                        throw new Exception("Expects additional information to start with hexadecimal value. Text: \n" + line + "\n");
                    }
#if false
                    // additional information skipped
                    string strValue = parts[0];
                    string remainder = line.Substring(line.IndexOf(strValue) + strValue.Length).Trim();
                    currentInfo.AdditionalInfos.Add(remainder, strValue);
#endif
                }
            }
            return imports;
        }

        internal class DisasmParser
        {
            private FilePath _callerFile;
            private IList<string> _lines;
            private int _lineAfterHeader;
            private HashSet<string> _funcNames = new HashSet<string>();
            private HashSet<int> _funcLineNums = new HashSet<int>();
            private Dictionary<string, List<int>> _funcLines = new Dictionary<string, List<int>>();
            private Dictionary<string, int> _importedNames = new Dictionary<string, int>();
            private Dictionary<string, int> _rawByteCounts = new Dictionary<string, int>();
            private List<CallEntry> _calls = new List<CallEntry>();

            public List<CallEntry> Calls => _calls;

            public DisasmParser(FilePath callerFile, IList<string> lines, IList<string> importedNames)
            {
                _callerFile = callerFile;
                _lines = lines;
                for (int index = 0; index < importedNames.Count; ++index)
                {
                    _importedNames.Add(importedNames[index], index);
                }
            }

            public void Run()
            {
                SkipHeaders();
                ParseFunctionNames();
                ParseDisasm();
            }

            private void SkipHeaders()
            {
                int headerIndicator = 0;
                for (int lineIndex = 0; lineIndex < _lines.Count; ++lineIndex)
                {
                    string line = _lines[lineIndex];
                    if (headerIndicator < 2)
                    {
                        if (line.StartsWith("Dump of file"))
                        {
                            headerIndicator++;
                        }
                        else if (line.StartsWith("File Type:"))
                        {
                            headerIndicator++;
                        }
                        continue;
                    }
                    if (line.EndsWith(":"))
                    {
                        _lineAfterHeader = lineIndex;
                        break;
                    }
                }
            }

            private void ParseFunctionNames()
            {
                for (int lineIndex = _lineAfterHeader; lineIndex < _lines.Count; ++lineIndex)
                {
                    string line = _lines[lineIndex];
                    if (line.EndsWith(":"))
                    {
                        string funcName = line.Substring(0, line.Length - 1);
                        _funcNames.Add(funcName);
                        _funcLineNums.Add(lineIndex);
                        _funcLines.AppendValue(funcName, lineIndex);
                    }
                }
            }

            private void ParseDisasm()
            {
                char[] spaceChars = new char[] { ' ', '\t' };
                char[] splitChars = new char[] { ' ', ',', '[', '+', ']', '-' };
                string currentFuncName = string.Empty;
                const ulong NotRVA = ulong.MaxValue;
                bool HasRVA(ulong rva)
                {
                    return (rva != NotRVA);
                };
                ulong currentFuncFirstRVA = NotRVA;
                ulong currentFuncLastRVA = NotRVA;
                for (int lineIndex = _lineAfterHeader; lineIndex < _lines.Count; ++lineIndex)
                {
                    string line = _lines[lineIndex];
                    if (_funcLineNums.Contains(lineIndex))
                    {
                        currentFuncName = line.TrimEnd(':');
                        currentFuncFirstRVA = NotRVA;
                        currentFuncLastRVA = NotRVA;
                        continue;
                    }
                    if (string.IsNullOrEmpty(currentFuncName))
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(line))
                    {
                        currentFuncName = string.Empty;
                        currentFuncFirstRVA = NotRVA;
                        currentFuncLastRVA = NotRVA;
                        continue;
                    }
                    string[] parts = line.Split(spaceChars, StringSplitOptions.RemoveEmptyEntries);
                    for (int partIndex = 0; partIndex < parts.Length; ++partIndex)
                    {
                        string part = parts[partIndex];
                        if (partIndex == 0 &&
                            IsRVA(part))
                        {
                            ulong rva = ulong.Parse(part.TrimEnd(':'), System.Globalization.NumberStyles.AllowHexSpecifier);
                            currentFuncLastRVA = rva;
                            if (!HasRVA(currentFuncFirstRVA))
                            {
                                currentFuncFirstRVA = rva;
                            }
                        }
                        else if (IsDisasmByte(part))
                        {
                            // ======
                            // Note: for "long" instructions (instructions that take more-than-usual
                            // number of bytes to encode), such instruction bytes might spill into
                            // next line. Therefore, the value of disasmByteCount might be increased
                            // on line two.
                            // ======
                            // Note: if the RVA space contains multiple copies of the same function,
                            // _rawByteCounts[] will contain the cumulative total.
                            // ======
                            // Note: currently, filler bytes ("CC") are also included.
                            // Note that only those bytes that start on an instruction boundary are
                            // truly filler bytes. If the byte value "CC" occurs in the middle of an
                            // instruction, such byte value shall not be treated as filler bytes.
                            // ======
                            _rawByteCounts[currentFuncName]++;
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
                                string calleeName = codePart;
                                if (_importedNames.ContainsKey(calleeName) ||
                                    _funcNames.Contains(calleeName))
                                {
                                    _calls.Add(new CallEntry(_callerFile, currentFuncName, calleeName));
                                }
                            }
                        }
                    }
                }
            }
        }

        public static List<CallEntry> GetCallsFromDisasm(FilePath targetFile, ImportTable importTable, ExportTable exportTable)
        {
            var invoker = new DumpBinInvoker();
            invoker.Arguments.Add("/DISASM");
            invoker.Arguments.Add(targetFile.FullName);
            invoker.Run();
            DisasmParser parsedResult = new DisasmParser(targetFile, invoker.Outputs, importTable.Symbols);
            parsedResult.Run();
            return parsedResult.Calls;
        }

        private static bool IsHexString(string s)
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

        private static bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        private static bool IsDecimalString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException("Input should contain one or more non-whitespace characters.");
            }
            foreach (char c in s.Trim())
            {
                if (!IsDecimalChar(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsDecimalChar(char c)
        {
            return (c >= '0' && c <= '9');
        }

        private static bool IsDisasmByte(string s)
        {
            return IsTwoHexChars(s);
        }

        private static bool IsTwoHexChars(string s)
        {
            return (s.Length == 2 && IsHexChar(s[0]) && IsHexChar(s[1]));
        }

        private static bool IsRVA(string s)
        {
            s = s.TrimEnd(':');
            return (s.Length >= 8) && IsHexString(s);
        }
    }
}

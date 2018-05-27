using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
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
    }
}

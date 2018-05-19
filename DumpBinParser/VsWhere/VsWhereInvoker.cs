using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace DumpBinParser.VsWhere
{
    public class VsWhereInvoker
    {
        public static class Internals
        {
            public static string ExePathDefault
            {
                get
                {
                    string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    return Path.Combine(pf86, @"Microsoft Visual Studio\Installer\vswhere.exe");
                }
            }
        }

        // Path to "vswhere.exe", can be modified if needed.
        public string ExePath
        {
            get;
            set;
        } = Internals.ExePathDefault;

        public List<string> OutputLines
        {
            get;
        } = new List<string>();

        public string VsInstallationPath
        {
            get;
            private set;
        }

        public VsWhereInvoker()
        {
        }

        public void Run()
        {
            if (!File.Exists(ExePath))
            {
                throw new FileNotFoundException("Cannot launch vswhere.exe", ExePath);
            }
            RunProcess();
            ParseOutput();
        }

        private void RunProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo(ExePath)
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
            string patternInstallationPath = "installationPath:";
            foreach (string s in OutputLines)
            {
                if (s.Contains(patternInstallationPath))
                {
                    VsInstallationPath = s.Replace(patternInstallationPath, "").Trim();
                }
            }
            if (string.IsNullOrEmpty(VsInstallationPath))
            {
                throw new Exception("Output from vswhere.exe does not mention installation path.");
            }
            if (!Directory.Exists(VsInstallationPath))
            {
                throw new DirectoryNotFoundException("Installation path reported by vswhere.exe does not exist: " + VsInstallationPath);
            }
        }
    }
}

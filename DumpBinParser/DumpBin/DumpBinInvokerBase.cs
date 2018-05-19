using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace DumpBinParser.DumpBin
{
    public abstract class DumpBinInvokerBase
    {
        /// <summary>
        /// Path to "dumpbin.exe". When left empty (default), the <code>Run()</code> method
        /// will try to find the path to this tool using the "vswhere.exe" tool.
        /// To override, assign this property after construction and before 
        /// calling the <code>Run()</code> method.
        /// </summary>
        public string ExePath
        {
            get;
            set;
        } = String.Empty;

        /// <summary>
        /// This method needs to be called by the concrete derived class immediately before
        /// the "dumpbin.exe" utility is invoked.
        /// </summary>
        protected void EnsureExePathSet()
        {
            if (string.IsNullOrEmpty(ExePath))
            {
                var vsWhere = new VsWhere.VsWhereInvoker();
                vsWhere.Run();
                ExePath = Path.Combine(vsWhere.VsInstallationPath, @"VC\Tools\MSVC\14.13.26128\bin\Hostx64\x64\dumpbin.exe");
            }
            if (!ExePath.ToLowerInvariant().Contains("dumpbin"))
            {
                throw new Exception("Invalid path for dumpbin.exe");
            }
            if (!File.Exists(ExePath))
            {
                throw new FileNotFoundException("Cannot invoke dumpbin.exe", ExePath);
            }
        }

        protected static string EnsurePathQuoted(string s)
        {
            if (!s.StartsWith("\"") && !s.EndsWith("\""))
            {
                return ("\"" + s + "\"");
            }
            return s;
        }
    }
}

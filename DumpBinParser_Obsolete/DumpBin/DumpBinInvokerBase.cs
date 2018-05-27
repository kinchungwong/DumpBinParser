using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace DumpBinParser.DumpBin
{
    [Obsolete("To be replaced with a call to DumpBinInvoker")]
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
        /// The external process on which "dumpbin.exe" was executed.
        /// </summary>
        public Utility.ProcessInvoker Invoker
        {
            get;
            protected set;
        }

        protected static readonly Lazy<string> _lazyFoundExePath = new Lazy<string>(() => {
            var vsWhere = new VsWhereInvoker();
            vsWhere.Run();
            return Path.Combine(vsWhere.VsInstallationPath, @"VC\Tools\MSVC\14.13.26128\bin\Hostx64\x64\dumpbin.exe");
        });

        /// <summary>
        /// This method needs to be called by the concrete derived class immediately before
        /// the "dumpbin.exe" utility is invoked.
        /// </summary>
        protected void EnsureExePathSet()
        {
            if (string.IsNullOrEmpty(ExePath))
            {
                ExePath = _lazyFoundExePath.Value;
            }
            else if (!ExePath.ToLowerInvariant().Contains("dumpbin"))
            {
                throw new Exception("Invalid path for dumpbin.exe");
            }
            if (!File.Exists(ExePath))
            {
                throw new FileNotFoundException("Cannot invoke dumpbin.exe", ExePath);
            }
        }
    }
}

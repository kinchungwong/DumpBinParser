using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class DumpBinInvoker
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
        } = new Utility.ProcessInvoker();

        /// <summary>
        /// <para>
        /// Arguments to be passed into "dumpbin.exe"
        /// </para>
        /// <para>
        /// Arguments which are paths do not need extra quoting; quoting will 
        /// be added automatically if the string contains space characters.
        /// </para>
        /// </summary>
        public IList<string> Arguments
        {
            get
            {
                return Invoker.Arguments;
            }
        }

        public IList<string> Outputs
        {
            get
            {
                return Invoker.OutputText;
            }
        }

        /// <summary>
        /// Runs "dumpbin.exe" with the arguments.
        /// </summary>
        public void Run()
        {
            EnsureExePathSet();
            if (Arguments.Count == 0)
            {
                throw new InvalidOperationException("Arguments list should not be empty.");
            }
            Invoker.ExePath = ExePath;
            using (Invoker)
            {
                Invoker.Run();
            }
        }

        private static readonly Lazy<string> _lazyFoundExePath = new Lazy<string>(() => {
            var vsWhere = new VsWhereInvoker();
            vsWhere.Run();
            return Path.Combine(vsWhere.VsInstallationPath, @"VC\Tools\MSVC\14.13.26128\bin\Hostx64\x64\dumpbin.exe");
        });

        /// <summary>
        /// This method needs to be called by the concrete derived class immediately before
        /// the "dumpbin.exe" utility is invoked.
        /// </summary>
        private void EnsureExePathSet()
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

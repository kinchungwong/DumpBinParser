using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    /// <summary>
    /// <para>
    /// Maintains the list of search paths for binary files.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The word "binary" refers to files that contain binary code (compiled executable 
    /// code). It is not related to "binary tree".
    /// </remarks>
    public class BinarySearchPaths
    {
        public HashSet<string> Directories
        {
            get;
        } = new HashSet<string>();

        public BinarySearchPaths()
        {
        }

        public void InitializeFromCurrentEnvironment()
        {
            char[] sep = new char[] { ';' };
            Directories.Add(Environment.SystemDirectory);
            Directories.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));
            foreach (string s in Environment.GetEnvironmentVariable("PATH").Split(sep, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Directory.Exists(s))
                {
                    Directories.Add(s);
                }
            }
        }

        public List<string> FindFile(string filename)
        {
            var results = new List<string>();
            foreach (string dir in Directories)
            {
                string s = Path.Combine(dir, filename);
                if (File.Exists(s))
                {
                    results.Add(s);
                }
            }
            return results;
        }
    }
}

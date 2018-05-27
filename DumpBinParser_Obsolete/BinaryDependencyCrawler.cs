using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
#if false
    public class BinaryDependencyCrawler
    {
        public BinaryDependency BinaryDependency
        {
            get;
            set;
        } = new BinaryDependency();

        public BinarySearchPaths BinarySearchPaths
        {
            get;
            set;
        } = new BinarySearchPaths();

        public Queue<string> UnprocessedBinaries
        {
            get;
        } = new Queue<string>();

        public BinaryDependencyCrawler()
        {
            BinarySearchPaths.InitializeFromCurrentEnvironment();
        }

        public void Add(string binaryFilename)
        {
            if (Path.IsPathRooted(binaryFilename))
            {
                BinarySearchPaths.Directories.Add(Path.GetDirectoryName(binaryFilename));
            }
            UnprocessedBinaries.Enqueue(binaryFilename);
            Process();
        }

        public void Process()
        {
            while (UnprocessedBinaries.Count > 0)
            {
                string binaryFilename = UnprocessedBinaries.Dequeue();
                var results = DumpBin.DumpBinProcessor.GetDependents(binaryFilename);
                foreach (string s in results)
                {
                    BinaryDependency.AddDependency(binaryFilename, s);
                    var candidates = BinarySearchPaths.FindFile(s);
                    foreach (var candidate in candidates)
                    {
                        var newKey = BinaryDependency.AddOrGetKey(candidate);
                        if (!BinaryDependency.Dependencies.ContainsKey(newKey))
                        {
                            UnprocessedBinaries.Enqueue(candidate);
                        }
                    }
                }
            }
        }
    }
#endif
}

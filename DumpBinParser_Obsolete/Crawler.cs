using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    using MultiValueDictionaryExtensions;

    public class Crawler
    {
        /// <summary>
        /// Search paths
        /// </summary>
        public HashSet<string> SearchPaths
        {
            get;
        } = new HashSet<string>(new PathKeyComparer());

        /// <summary>
        /// An integer value, taken to be the count of the SearchPaths. 
        /// This value is used to check whether new search paths have been added,
        /// in order to determine whether to re-run the search for files.
        /// </summary>
        public int SearchPathVersion => SearchPaths.Count;

        /// <summary>
        /// Given the lower case file name without directory, return the list of 
        /// candidate full paths.
        /// </summary>
        public Dictionary<string, FileCandidates> AllFileCandidates
        {
            get;
        } = new Dictionary<string, FileCandidates>(new FilenameKeyComparer());

        /// <summary>
        /// Captures all information from DLL dependence generated using DUMPBIN
        /// </summary>
        public Dictionary<string, DependenceRecord> DependenceRecords
        {
            get;
        } = new Dictionary<string, DependenceRecord>(new PathKeyComparer());

        /// <summary>
        /// All of the exported functions from all files
        /// </summary>
        public Dictionary<string, FileExports> AllFileExports
        {
            get;
        } = new Dictionary<string, FileExports>(new PathKeyComparer());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public List<string> ResolveFilename(string filename)
        {
            string pathlessFilename = Path.GetFileName(filename);
            if (!AllFileCandidates.TryGetValue(pathlessFilename, out FileCandidates fileCandidates))
            {
                fileCandidates = new FileCandidates()
                {
                    PathlessFilename = pathlessFilename,
                    SearchPathVersion = 0,
                };
                AllFileCandidates.Add(pathlessFilename, fileCandidates);
            }
            if (Path.IsPathRooted(filename))
            {
                // If this function is called with a rooted path, it is prevented from
                // conducting a search using the current list of search paths.
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Exists)
                {
                    string fullname = fileInfo.FullName;
                    fileCandidates.FullPathCandidates.Add(fullname);
                    return new List<string> { fullname };
                }
                else
                {
                    return new List<string>();
                }
            }
            else if (fileCandidates.SearchPathVersion < SearchPathVersion)
            {
                // Conducting a search using the current list of search paths.
                // This section of code is only entered if search (using the same 
                // list of search paths) has not been done before.
                foreach (string dir in SearchPaths)
                {
                    var fileInfo = new FileInfo(Path.Combine(dir, filename));
                    if (fileInfo.Exists)
                    {
                        string fullname = fileInfo.FullName;
                        fileCandidates.FullPathCandidates.Add(fullname);
                    }
                }
                // Mark the completion of this search, so that if the same filename
                // is searched again while SearchPaths stays the same, no redundant 
                // work is performed.
                fileCandidates.SearchPathVersion = SearchPaths.Count;
            }
            return new List<string>(fileCandidates.FullPathCandidates);
        }

        /// <summary>
        /// <para>
        /// Returns the list of dependent binary files for the given binary file.
        /// </para>
        /// <para>
        /// This function has to distinct modes of operation. If the input filename is a
        /// complete path, that file alone is inspected. If it is not a complete path,
        /// a list of candidate binary files are located among the search paths, and each of 
        /// these will have their dependents enumerated.
        /// </para>
        /// <para>
        /// Inspection results for each binary file are cached, to prevent endless loop of
        /// recalculations.
        /// </para>
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="addDirectoryToSearchPaths"></param>
        /// <returns>
        /// The list of dependent files. This list contains the pathless file names.
        /// To translate into full paths, see <see cref="AllFileCandidates"/>.
        /// </returns>
        public List<string> GetDependents(string filename, bool addDirectoryToSearchPaths = false)
        {
            if (addDirectoryToSearchPaths &&
                Path.IsPathRooted(filename) && 
                File.Exists(filename))
            {
                SearchPaths.Add(Path.GetDirectoryName(filename));
            }
            List<string> candidates = ResolveFilename(filename);
            HashSet<string> pathlessDependents = new HashSet<string>(new FilenameKeyComparer());
            foreach (string candidate in candidates)
            {
                var fileInfo = new FileInfo(candidate);
                var fullName = fileInfo.FullName;
                if (DependenceRecords.ContainsKey(fullName))
                {
                    foreach (string dependent in DependenceRecords[fullName].Dependents)
                    {
                        pathlessDependents.Add(dependent);
                    }
                    continue;
                }
                var results = DumpBinProcessor.GetDependents(fullName);
                var record = new DependenceRecord()
                {
                    StartingFilePath = fullName,
                    Dependents = results
                };
                DependenceRecords.Add(fullName, record);
                foreach (string result in results)
                {
                    pathlessDependents.Add(result);
                }
            }
            return new List<string>(pathlessDependents);
        }

        /// <summary>
        /// Calls GetDependents on the given filename, and then resolves the dependents
        /// and find further (second order and up) dependents.
        /// </summary>
        /// <param name="filename"></param>
        public void GetDependentsRecursive(string filename)
        {
            GetDependents(filename, true);
            while (true)
            {
                HashSet<string> unresolvedDependents = new HashSet<string>(new FilenameKeyComparer());
                foreach (FileCandidates fileCandidates in AllFileCandidates.Values)
                {
                    foreach (string candidate in fileCandidates.FullPathCandidates)
                    {
                        var dependents = GetDependents(candidate);
                        foreach (string dependent in dependents)
                        {
                            if (!AllFileCandidates.ContainsKey(dependent))
                            {
                                unresolvedDependents.Add(dependent);
                            }
                        }
                    }
                }
                if (unresolvedDependents.Count == 0)
                {
                    break;
                }
                foreach (string dependent in unresolvedDependents)
                {
                    ResolveFilename(dependent);
                }
            }
        }

        public FileExports GetFileExports(string filename, bool addDirectoryToSearchPaths = false)
        {
            if (addDirectoryToSearchPaths &&
                Path.IsPathRooted(filename) &&
                File.Exists(filename))
            {
                SearchPaths.Add(Path.GetDirectoryName(filename));
            }
            List<string> candidates = ResolveFilename(filename);
            FileExports firstResult = null;
            foreach (string candidate in candidates)
            {
                var fileInfo = new FileInfo(candidate);
                var fullName = fileInfo.FullName;
                if (AllFileExports.ContainsKey(fullName))
                {
                    return AllFileExports[fullName];
                }
                var records = DumpBinProcessor_Obsolete.GetExports_Obsolete(fullName);
                var fileExports = new FileExports()
                {
                    StartingFilePath = fullName
                };
                foreach (var record in records)
                {
                    fileExports.Records.Add(record);
                    if (!string.IsNullOrEmpty(record.Prototype) &&
                        !fileExports.LookupByPrototype.ContainsKey(record.Prototype))
                    {
                        fileExports.LookupByPrototype.Add(record.Prototype, record);
                    }
                    if (!string.IsNullOrEmpty(record.DecoratedName) &&
                        !fileExports.LookupByDecoratedName.ContainsKey(record.DecoratedName))
                    {
                        fileExports.LookupByDecoratedName.Add(record.DecoratedName, record);
                    }
                }
                AllFileExports.Add(fullName, fileExports);
                if (firstResult == null)
                {
                    firstResult = fileExports;
                }
            }
            return firstResult;
        }

        /// <summary>
        /// Initializes search path from the value of the environment variable "PATH".
        /// </summary>
        public void InitializeSearchPathFromCurrentEnvironment()
        {
            char[] sep = new char[] { ';' };
            foreach (string s in Environment.GetEnvironmentVariable("PATH").Split(sep, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Directory.Exists(s))
                {
                    SearchPaths.Add(s);
                }
            }
        }
    }

    /// <summary>
    /// Allows the use of file names as keys, normalized to lower case, and 
    /// with the directory part removed.
    /// </summary>
    public class FilenameKeyComparer : IEqualityComparer<string>
    {
        public static string Transform(string s)
        {
            return Path.GetFileName(s).ToLowerInvariant();
        }

        bool IEqualityComparer<string>.Equals(string s1, string s2)
        {
            s1 = Transform(s1);
            s2 = Transform(s2);
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        int IEqualityComparer<string>.GetHashCode(string s)
        {
            s = Transform(s);
            return s.GetHashCode();
        }
    }

    /// <summary>
    /// Allows use of paths as keys in Dictionary and HashSet
    /// while normalizing paths into lower case.
    /// </summary>
    public class PathKeyComparer : IEqualityComparer<string>
    {
        public static string Transform(string s)
        {
            return s.ToLowerInvariant();
        }

        bool IEqualityComparer<string>.Equals(string s1, string s2)
        {
            s1 = Transform(s1);
            s2 = Transform(s2);
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        int IEqualityComparer<string>.GetHashCode(string s)
        {
            s = Transform(s);
            return s.GetHashCode();
        }
    }

    /// <summary>
    /// <para>
    /// The list of dependent binaries for a given starting binary file.
    /// </para>
    /// <para>
    /// Note that the starting binary file must be specified as a full path;
    /// the list of dependents are specified as pathless file names.
    /// </para>
    /// </summary>
    public class DependenceRecord
    {
        public string StartingFilePath
        {
            get;
            set;
        }

        public IList<string> Dependents
        {
            get;
            set;
        }

        public override int GetHashCode()
        {
            return StartingFilePath.GetHashCode();
        }
    }

    /// <summary>
    /// Given the pathless file name, map to a list of candidates
    /// found on the search path.
    /// </summary>
    public class FileCandidates
    {
        /// <summary>
        /// The pathless file name. This is typically used as the lookup key for this record.
        /// </summary>
        public string PathlessFilename
        {
            get;
            set;
        }

        /// <summary>
        /// List of complete paths to files which match the pathless file name.
        /// </summary>
        public HashSet<string> FullPathCandidates
        {
            get;
        } = new HashSet<string>(new PathKeyComparer());

        /// <summary>
        /// An integer value, taken to be the count of the SearchPaths used to
        /// conduct the file search. This value is used to determine whether to
        /// re-run the search after new paths have been added to SearchPaths.
        /// </summary>
        public int SearchPathVersion
        {
            get;
            set;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool isFirst = true;
            foreach (string candidate in FullPathCandidates)
            {
                if (isFirst)
                {
                    sb.Append(" ");
                    isFirst = false;
                }
                else
                {
                    sb.Append("; ");
                }
                sb.Append(candidate);
            }
            sb.Append(" }");
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return PathlessFilename.GetHashCode();
        }
    }

    /// <summary>
    /// The list of functions exported by a binary file.
    /// </summary>
    public class FileExports
    {
        public string StartingFilePath
        {
            get;
            set;
        }

        public List<DumpBin.DumpBinExportsRecord> Records
        {
            get;
        } = new List<DumpBin.DumpBinExportsRecord>();

        public Dictionary<string, DumpBin.DumpBinExportsRecord> LookupByDecoratedName
        {
            get;
        } = new Dictionary<string, DumpBin.DumpBinExportsRecord>();

        public Dictionary<string, DumpBin.DumpBinExportsRecord> LookupByPrototype
        {
            get;
        } = new Dictionary<string, DumpBin.DumpBinExportsRecord>();

        public override string ToString()
        {
            return "{ " + StartingFilePath + ": " + Records.Count + " exports }";
        }
    }
}

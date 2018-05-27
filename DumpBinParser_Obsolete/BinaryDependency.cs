using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;
using DumpBinParser.MultiValueDictionaryExtensions;

namespace DumpBinParser
{
    public class BinaryDependency
    {
        public Dictionary<string, List<string>> Dependencies
        {
            get;
        } = MultiValueDictionaryMethods.Create<string, string>();

        public Dictionary<string, string> FileNameToFullPath
        {
            get;
        } = new Dictionary<string, string>();

        public string AddOrGetKey(string path)
        {
            string fileNamePart = Path.GetFileName(path).ToLowerInvariant();
            if (!FileNameToFullPath.ContainsKey(fileNamePart))
            {
                FileNameToFullPath.Add(fileNamePart, path);
            }
            return fileNamePart;
        }

        public void AddDependency(string consumer, string provider)
        {
            string consumerKey = AddOrGetKey(consumer);
            string providerKey = AddOrGetKey(provider);
            if (!string.Equals(consumerKey, providerKey, StringComparison.InvariantCulture))
            {
                Dependencies.AppendValue(consumer, provider);
            }
        }
    }
}

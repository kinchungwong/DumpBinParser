using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class ImportEntry : IEquatable<ImportEntry>
    {
        public FilePath CalledFromFile
        {
            get;
            private set;
        }

        public FileIdentity CalledFromFileIdentity
        {
            get
            {
                return CalledFromFile.Identity;
            }
        }

        public FileHint ProviderFileHint
        {
            get;
            private set;
        }

        /// <summary>
        /// <para>
        /// Symbol is a string (alphanumeric plus some symbol characters) used to identify 
        /// a function. Typically, this is known as the "decorated" function name.
        /// </para>
        /// <para>
        /// Exported C++ functions are converted into a "decorated" function name resulting 
        /// in many extra alphanumeric character codes and symbol characters being inserted.
        /// </para>
        /// <para>
        /// Exported C functions consist of just the name of the function. Such function
        /// names are not "decorated", and the name does not convey the arguments or return 
        /// types. Since classes and namespaces do not exist in C, an exported C function
        /// cannot be a member of a class or a namespace.
        /// </para>
        /// </summary>
        public string Symbol
        {
            get;
            private set;
        }

        /// <summary>
        /// <para>
        /// The user-friendly short function name. This is without the namespace, class name,
        /// and decoration. The short function name does not convey the arguments, return types,
        /// template arguments, or visibility.
        /// </para>
        /// <para>
        /// For exported C functions, the short function name is usually unique. To the extent that
        /// it is non-unique, it is assumed that the execution of any such candidates result in
        /// the same outcome for all practical purposes.
        /// </para>
        /// <para>
        /// For exported C++ functions, the short function name is not unique, and cannot be used
        /// as a proper means of identification (matching) between caller and callee.
        /// </para>
        /// </summary>
        public string ShortFunctionName
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ImportEntry(FilePath calledFromFile, FileHint providerFileHint, string symbol, 
            string shortFunctionName)
        {
            CalledFromFile = calledFromFile;
            ProviderFileHint = providerFileHint;
            Symbol = symbol;
            ShortFunctionName = shortFunctionName;
        }

        public override string ToString()
        {
            return Symbol;
        }

        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        public bool Equals(ImportEntry other)
        {
            if (other == null)
            {
                return false;
            }
            return CalledFromFile.Equals(other.CalledFromFile) &&
                ProviderFileHint.Equals(other.ProviderFileHint) &&
                Symbol.Equals(other.Symbol);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ImportEntry);
        }
    }
}

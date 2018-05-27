using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpBinParser
{
    public class ExportEntry : IEquatable<ExportEntry>
    {
        public FilePath Provider
        {
            get;
            private set;
        }

        public FileIdentity ProviderIdentity
        {
            get
            {
                return Provider.Identity;
            }
        }

        /// <summary>
        /// For exported C++ functions, the DLL typically contains a human-readable
        /// string that is the C++ function prototype. For functions which are not 
        /// C++, this member may be set to <see cref="null"/> or <see cref="string.Empty"/>.
        /// </summary>
        public string FunctionPrototype
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
        /// The user-friendly short function name. This is without the namespace, class name,
        /// and decoration. The short function name does not convey the arguments, return types,
        /// template arguments, or visibility.
        /// </summary>
        public string ShortFunctionName
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExportEntry(FilePath provider, string functionPrototypeOrNull, string symbol, 
            string shortFunctionName)
        {
            Provider = provider;
            FunctionPrototype = functionPrototypeOrNull;
            Symbol = symbol;
            ShortFunctionName = shortFunctionName;
        }

        public override string ToString()
        {
            return Symbol + "(" + Provider.FileHint.PathlessFileName + ")";
        }

        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }

        public bool Equals(ExportEntry other)
        {
            if (other == null)
            {
                return false;
            }
            return Provider.Identity.Equals(other.Provider.Identity) &&
                string.Equals(Symbol, other.Symbol, StringComparison.Ordinal);
        }
    }
}

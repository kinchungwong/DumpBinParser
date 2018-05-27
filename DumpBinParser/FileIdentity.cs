using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace DumpBinParser
{
    public class FileIdentity : IEquatable<FileIdentity>
    {
        public string Checksum
        {
            get;
            private set;
        }

        public ulong ByteSize
        {
            get;
            private set;
        }

        public static FileIdentity ComputeFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }
            using (var stream = File.OpenRead(filename))
            {
                return ComputeFromStream(stream);
            }
        }

        public static FileIdentity ComputeFromStream(Stream stream)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            byte[] checksumBytes = sha1.ComputeHash(stream);
            string checksum = string.Join(string.Empty, (from byteValue in checksumBytes select byteValue.ToString("X2")).ToArray());
            ulong byteSize = checked((ulong)stream.Length);
            return new FileIdentity(checksum, byteSize);
        }

        public FileIdentity(string checksum, ulong byteSize)
        {
            Checksum = checksum;
            ByteSize = byteSize;
        }

        public bool Equals(FileIdentity other)
        {
            if (other == null)
            {
                return false;
            }
            return Checksum == other.Checksum &&
                ByteSize == other.ByteSize;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileIdentity);
        }

        public override int GetHashCode()
        {
            return Checksum.GetHashCode();
        }

        public override string ToString()
        {
            return Checksum;
        }
    }
}

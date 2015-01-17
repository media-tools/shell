using System;
using System.IO;
using System.Security.Cryptography;

namespace Shell.Common.Util
{
    public static class FileSystemUtilities
    {
        private static SHA256Managed crypt = new SHA256Managed ();

        public static HexString HashOfFile (string path)
        {
            using (Stream stream = File.Open (path: path, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.ReadWrite)) {
                return HexString.FromByteArray (crypt.ComputeHash (stream));
            }
        }
    }

    public struct HexString
    {
        public string Hash;

        public bool Equals (HexString other)
        {
            return Hash == other.Hash;
        }

        public override bool Equals (object obj)
        {
            if (obj != null && obj is HexString) {
                return Equals ((HexString)obj);
            }
            return false;
        }

        public override int GetHashCode ()
        {
            return Hash.GetHashCode ();
        }

        public static bool operator == (HexString c1, HexString c2)
        {
            return c1.Equals (c2);
        }

        public static bool operator != (HexString c1, HexString c2)
        {
            return !(c1 == c2);
        }

        public static HexString FromByteArray (byte[] bytes)
        {
            return new HexString () {
                Hash = ByteArrayToHexViaLookup32 (bytes)
            };
        }

        /**
         * http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa/24343727#24343727
         */

        private static readonly uint[] _lookup32 = CreateLookup32 ();

        private static uint[] CreateLookup32 ()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++) {
                string s = i.ToString ("X2");
                result [i] = ((uint)s [0]) + ((uint)s [1] << 16);
            }
            return result;
        }

        private static string ByteArrayToHexViaLookup32 (byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++) {
                var val = lookup32 [bytes [i]];
                result [2 * i] = (char)val;
                result [2 * i + 1] = (char)(val >> 16);
            }
            return new string (result);
        }

        public override string ToString ()
        {
            return Hash;
        }

        public string PrintShort { get { return Hash.Length >= 8 ? Hash.Substring (0, 8) : string.Empty; } }
    }
}


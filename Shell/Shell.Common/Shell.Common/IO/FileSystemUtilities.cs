using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Shell.Common.IO;

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

        public static string[] ToAbsolutePaths (IEnumerable<string> paths, bool silent = false)
        {
            List<string> result = new List<string> ();
            foreach (string path in paths) {
                try {
                    result.Add (Path.GetFullPath (path));
                } catch (Exception ex) {
                    if (!silent) {
                        Log.Error ("Invalid path: ", path);
                        Log.Error (ex);
                    }
                }
            }
            return result.ToArray ();
        }
    }

}

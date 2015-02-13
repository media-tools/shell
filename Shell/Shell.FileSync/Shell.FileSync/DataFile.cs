using System;
using System.IO;
using Shell.Common.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Shell.FileSync
{
    public class DataFile
    {
        public static bool DEEP_COMPARE = false;

        public FileInfo FileInfo { get; private set; }

        public string RelativePath { get; private set; }

        public string FullPath { get { return FileInfo.FullName; } }

        public string Name { get { return FileInfo.Name; } }

        public string Extension { get { return FileInfo.Extension; } }

        public long Length { get { return FileInfo.Length; } }

        private static SHA256Managed crypt = new SHA256Managed ();

        public DataFile (FileInfo fileInfo, Tree tree)
        {
            FileInfo = fileInfo;
            if (FullPath.StartsWith (tree.RootDirectory)) {
                RelativePath = FullPath.Replace (tree.RootDirectory, "").Trim ('/', '\\');
            } else {
                Log.Error ("File in not in Tree: file=", FullPath, ", tree=", tree.RootDirectory);
            }
        }

        public byte[] SHA256Hash ()
        {
            return crypt.ComputeHash (File.OpenRead (FullPath));
        }

        public bool ContentEquals (DataFile otherFile)
        {
            if (DEEP_COMPARE) {
                if (Length == otherFile.Length) {
                    byte[] sHash = this.SHA256Hash ();
                    byte[] dHash = otherFile.SHA256Hash ();
                    return sHash.SequenceEqual (dHash);
                } else {
                    return false;
                }
            } else {
                if (Length == otherFile.Length) {
                    /*if (Length < 5 * 1000 * 1000) {
                        byte[] sBytes = this.readFirstBytes ();
                        byte[] dBytes = otherFile.readFirstBytes ();
                        return sBytes.SequenceEqual (dBytes);
                    } else {*/
                    return true;
                    //}
                } else {
                    return false;
                }
            }
        }

        private byte[] readFirstBytes ()
        {
            try {
                byte[] buffer = new byte[512];
                using (FileStream fs = new FileStream (FullPath, FileMode.Open, FileAccess.Read)) {
                    fs.Read (buffer, 0, buffer.Length);
                    fs.Close ();
                }
                return buffer;
            } catch (Exception) {
                //Log.Debug ("Error in readFirstBytes (): ", ex.Message);
                //return new byte[0];
                throw;
            }
        }

        public TimeSpan GetWriteTimeDiff (DataFile otherFile)
        {
            return File.GetLastWriteTimeUtc (FullPath) - File.GetLastWriteTimeUtc (otherFile.FullPath);
        }

        public override string ToString ()
        {
            return string.Format ("{0}", RelativePath);
        }

        public override int GetHashCode ()
        {
            return RelativePath.GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as DataFile);
        }

        public bool Equals (DataFile obj)
        {
            return obj != null && GetHashCode () == obj.GetHashCode ();
        }
    }
}


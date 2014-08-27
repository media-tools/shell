using System;
using System.IO;
using Control.Common.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Control.FileSync
{
    public class DataFile
    {
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
            if (Length == otherFile.Length) {
                byte[] sHash = this.SHA256Hash ();
                byte[] dHash = otherFile.SHA256Hash ();
                return sHash.SequenceEqual (dHash);
            } else {
                return false;
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


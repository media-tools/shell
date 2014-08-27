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
        public string FullPath { get; private set; }

        public string RelativePath { get; private set; }

        public string Name { get; private set; }

        public string Extension { get; private set; }

        private static SHA256Managed crypt = new SHA256Managed ();

        public DataFile (FileInfo fileInfo, Tree tree)
        {
            FullPath = fileInfo.FullName;
            Name = fileInfo.Name;
            Extension = fileInfo.Extension;
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
            byte[] sHash = this.SHA256Hash ();
            byte[] dHash = otherFile.SHA256Hash ();
            return sHash.SequenceEqual (dHash);
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


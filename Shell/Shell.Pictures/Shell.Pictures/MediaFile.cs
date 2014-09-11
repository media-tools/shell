using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Shell.Common.IO;

namespace Shell.Pictures
{
    public abstract class MediaFile
    {
        public string FullPath { get; private set; }

        public string Name { get; private set; }

        public string Extension { get; private set; }

        public string RelativePath { get; private set; }

        public string AlbumPath { get; private set; }

        public MediaFile (FileInfo fileInfo, string root)
        {
            Debug.Assert (fileInfo.FullName.StartsWith (root), "file path is not in root directory (FullName=" + fileInfo.FullName + ",root=" + root + ")");
            FullPath = fileInfo.FullName;
            Name = fileInfo.Name;
            Extension = fileInfo.Extension;
            RelativePath = FullPath.Substring(root.Length).TrimStart('/', '\\');
            AlbumPath = Path.GetDirectoryName(RelativePath).Trim('/', '\\');
            Log.Debug ("Extension: ", fileInfo.Extension, ", AlbumPath: ", AlbumPath, ", Name: ", Name);
        }

        protected static bool IsValidFile (FileInfo fileInfo, HashSet<string> fileEndings)
        {
            return fileEndings.Contains (fileInfo.Extension.ToLower ());
        }
    }
}


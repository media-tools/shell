using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Media.Content;
using Shell.Common.Util;
using Shell.Common.Shares;

namespace Shell.Media.Files
{
    public class MediaFile : ValueObject<MediaFile>
    {
        public string FullPath { get; private set; }

        public string Filename { get; private set; }

        public string Extension { get; private set; }

        public string RelativePath { get; private set; }

        public string AlbumPath { get; private set; }

        public Medium Medium { get; private set; }

        public MediaShare Share { get; private set; }

        private static MediaFileLibrary lib = new MediaFileLibrary ();

        public bool IsDeleted { get; set; }

        public MediaFile (string fullPath, MediaShare share)
        {
            Debug.Assert (fullPath.StartsWith (share.RootDirectory), "file path is not in root directory (FullName=" + fullPath + ",root=" + share.RootDirectory + ")");
            Share = share;
            FullPath = fullPath;
            Filename = Path.GetFileName (fullPath);
            Extension = Path.GetExtension (fullPath);
            RelativePath = MediaShareUtilities.GetRelativePath (fullPath: fullPath, share: share);
            AlbumPath = MediaShareUtilities.GetAlbumPath (fullPath: fullPath, share: share);
        }

        public MediaFile (string fullPath, HexString hash, MediaShare share)
            : this (fullPath: fullPath, share: share)
        {
            Medium medium;
            if (share.GetMediumByHash (hash: hash, medium: out medium)) {
                medium.AddFile (mediaFile: this);
                Medium = medium;
            } else {
                Log.Debug ("not cached: medium by hash: ", fullPath);
                Index ();
            }
        }

        public long Size {
            get {
                try {
                    return new FileInfo (FullPath).Length;
                } catch (Exception ex) {
                    Log.Error ("Error while getting file size: ", FullPath, ": ", ex.Message);
                    throw;
                }
            }
        }

        public void Index ()
        {
            Medium medium;
            if (Picture.IsValidFile (fullPath: FullPath)) {
                medium = new Picture (fullPath: FullPath);
            } else if (Video.IsValidFile (fullPath: FullPath)) {
                medium = new Video (fullPath: FullPath);
            } else if (Audio.IsValidFile (fullPath: FullPath)) {
                medium = new Audio (fullPath: FullPath);
            } else if (Document.IsValidFile (fullPath: FullPath)) {
                medium = new Document (fullPath: FullPath);
            } else {
                throw new ArgumentException ("[MediaFile] Unknown file: " + FullPath);
            }

            Medium cachedMedium;
            if (Share.GetMediumByHash (hash: medium.Hash, medium: out cachedMedium)) {
                medium = cachedMedium;
            } else {
                Share.AddMedium (media: medium);
            }

            medium.Index (fullPath: FullPath);

            medium.AddFile (mediaFile: this);
            Medium = medium;
        }

        public bool IsCompletelyIndexed {
            get {
                if (Medium == null)
                    return false;
                return Medium.IsCompletelyIndexed;
            }
        }

        public static void RunIndexHooks (ref string fullPath)
        {
            // does the file have no proper filename?
            if (MediaFile.HasNoFileEnding (fullPath: fullPath)) {
                string fileEnding;
                // determine the best file ending
                if (MediaFile.DetermineFileEnding (fullPath: fullPath, fileEnding: out fileEnding)) {
                    // rename the file
                    MediaFile.AddFileEnding (fullPath: ref fullPath, fileEnding: fileEnding);
                }
            }

            // is the file ending not completely lower case?
            MediaFile.ChangeFileEndingToLowerCase (fullPath: ref fullPath);

            // does the file name contain characters that are not allowed in NTFS?
            MediaFile.RenameFilePlatformIndependent (fullPath: ref fullPath);
        }

        public static bool RenameFilePlatformIndependent (ref string fullPath)
        {
            if (Path.GetFileName (fullPath).Contains (":")) {
                string oldPath = fullPath;
                string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileName (oldPath).Replace (":", "_");
                return RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);
            }
            if (Path.GetFileName (fullPath).StartsWith ("_")) {
                string oldPath = fullPath;
                string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileName (oldPath).Trim ('_');
                return RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);
            }
            return false;
        }

        public static bool ChangeFileEndingToLowerCase (ref string fullPath)
        {
            if (Path.GetFileNameWithoutExtension (fullPath).Length >= 1 && Path.GetExtension (fullPath) != Path.GetExtension (fullPath).ToLower ()) {
                string oldPath = fullPath;
                string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileNameWithoutExtension (oldPath) + Path.GetExtension (oldPath).ToLower ();
                return RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);
            }
            return false;
        }

        public static bool RenamePath (ref string fullPath, string oldPath, string newPath)
        {
            try {
                Log.Message ("Rename file: ", Path.GetFileName (oldPath), " => ", Path.GetFileName (newPath));
                if (File.Exists (newPath) && File.Exists (oldPath)) {
                    File.Delete (newPath);
                }
                File.Move (sourceFileName: oldPath, destFileName: newPath);
                fullPath = newPath;
                return true;
            } catch (IOException ex) {
                Log.Error (ex);
            }
            return false;
        }

        public static bool IsValidFile (string fullPath)
        {
            return Picture.IsValidFile (fullPath: fullPath) || Audio.IsValidFile (fullPath: fullPath) || Video.IsValidFile (fullPath: fullPath) || Document.IsValidFile (fullPath: fullPath);
        }

        public static bool IsIgnoredFile (string fullPath)
        {
            return Path.GetFileName (fullPath) == CommonShare<MediaShare>.CONFIG_FILENAME;
        }

        public static bool HasNoFileEnding (string fullPath)
        {
            bool hasNoEnding;
            if (MediaFile.IsValidFile (fullPath: fullPath)) {
                hasNoEnding = false;

            } else {
                string fileName = fullPath.Split ('/').Last ();
                string lastPart = fileName.Split ('.').Last ();
                bool containsLetters = lastPart.Any (x => char.IsLetter (x));
                bool containsWhitespaces = lastPart.Any (x => char.IsWhiteSpace (x));
                bool containsPunctuation = lastPart.Any (x => char.IsPunctuation (x));

                hasNoEnding = !containsLetters || containsWhitespaces || containsPunctuation;

                if (hasNoEnding) {
                    Log.Debug ("File has no extension: ", fileName);
                    Log.Indent++;
                    Log.Debug ("HasNoFileEnding: result=", hasNoEnding, ", fileName=", fileName, ", lastPart=", lastPart, ", containsLetters=", containsLetters, ", containsWhitespaces=", containsWhitespaces, ", containsPunctuation=", containsPunctuation);
                    Log.Indent--;
                }
            }
            return hasNoEnding;
        }

        public static bool DetermineFileEnding (string fullPath, out string fileEnding)
        {
            string mimeType = lib.GetMimeTypeByExternalCall (fullPath: fullPath);
            fileEnding = lib.GetExtensionByMimeType (mimeType: mimeType);

            Log.Debug ("DetermineFileEnding: mimeType=", mimeType, ", fileEnding=", fileEnding);

            return fileEnding != null;
        }

        public static void AddFileEnding (ref string fullPath, string fileEnding)
        {
            string oldFullPath = fullPath;
            string newFullPath = fullPath + fileEnding;
            File.Move (sourceFileName: oldFullPath, destFileName: newFullPath);
            fullPath = newFullPath;

            Log.Message ("Rename file: old full path = ", oldFullPath);
            Log.Message ("             new full path = ", newFullPath);
        }

        public override string ToString ()
        {
            return string.Format ("[MediaFile: Name={0}, Extension={1}, AlbumPath={2}]", Filename, Extension, AlbumPath);
        }

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { RelativePath };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<MediaFile>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (MediaFile a, MediaFile b)
        {
            return ValueObject<MediaFile>.Equality (a, b);
        }

        public static bool operator != (MediaFile a, MediaFile b)
        {
            return ValueObject<MediaFile>.Inequality (a, b);
        }
    }
}


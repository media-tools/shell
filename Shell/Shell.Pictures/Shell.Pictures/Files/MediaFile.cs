using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Pictures.Content;
using Shell.Common.Util;

namespace Shell.Pictures.Files
{
    public class MediaFile : ValueObject<MediaFile>
    {
        public string FullPath { get; private set; }

        public string Name { get; private set; }

        public string Extension { get; private set; }

        public string RelativePath { get; private set; }

        public string AlbumPath { get; private set; }

        public Medium Medium { get; private set; }

        public PictureShare Share { get; private set; }

        private static MediaFileLibrary lib = new MediaFileLibrary ();

        public MediaFile (string fullPath, PictureShare share)
        {
            Debug.Assert (fullPath.StartsWith (share.RootDirectory), "file path is not in root directory (FullName=" + fullPath + ",root=" + share.RootDirectory + ")");
            Share = share;
            FullPath = fullPath;
            Name = Path.GetFileName (fullPath);
            Extension = Path.GetExtension (fullPath);
            RelativePath = PictureShareUtilities.GetRelativePath (fullPath: fullPath, share: share);
            AlbumPath = PictureShareUtilities.GetAlbumPath (fullPath: fullPath, share: share);
        }

        public MediaFile (string fullPath, HexString hash, PictureShare share)
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

        public static bool IsValidFile (string fullPath)
        {
            return Picture.IsValidFile (fullPath: fullPath) || Audio.IsValidFile (fullPath: fullPath) || Video.IsValidFile (fullPath: fullPath) || Document.IsValidFile (fullPath: fullPath);
        }

        public static bool HasNoEnding (string fullPath)
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

                Log.Debug ("HasNoEnding: result=", hasNoEnding, ", fileName=", fileName, ", lastPart=", lastPart, ", containsLetters=", containsLetters, ", containsWhitespaces=", containsWhitespaces, ", containsPunctuation=", containsPunctuation);
            }
            return hasNoEnding;
        }

        public static bool DetermineFileEnding (string fullPath, out string fileEnding)
        {
            string mimeType = lib.GetMimeType (fullPath: fullPath);
            if (Picture.MIME_TYPES.ContainsKey (mimeType)) {
                fileEnding = Picture.MIME_TYPES [mimeType];
            } else if (Audio.MIME_TYPES.ContainsKey (mimeType)) {
                fileEnding = Audio.MIME_TYPES [mimeType];
            } else if (Video.MIME_TYPES.ContainsKey (mimeType)) {
                fileEnding = Video.MIME_TYPES [mimeType];
            } else if (Document.MIME_TYPES.ContainsKey (mimeType)) {
                fileEnding = Document.MIME_TYPES [mimeType];
            } else {
                fileEnding = null;
            }

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
            return string.Format ("[MediaFile: Name={0}, Extension={1}, AlbumPath={2}]", Name, Extension, AlbumPath);
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


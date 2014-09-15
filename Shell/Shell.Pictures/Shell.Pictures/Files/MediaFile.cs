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
                Log.Debug ("cached: medium by hash");
                medium.AddFile (mediaFile: this);
                Medium = medium;
            }
        }

        public void Index (bool cached = true, Album album = null)
        {
            Medium medium;
            if (Picture.IsValidFile (fullPath: FullPath)) {
                medium = new Picture (fullPath: FullPath);
                Share.Add (media: medium);
            } else if (Video.IsValidFile (fullPath: FullPath)) {
                medium = new Video (fullPath: FullPath);
                Share.Add (media: medium);
            } else if (Audio.IsValidFile (fullPath: FullPath)) {
                medium = new Audio (fullPath: FullPath);
                Share.Add (media: medium);
            } else {
                throw new ArgumentException ("[MediaFile] Unknown file: " + FullPath);
            }
            medium.AddFile (mediaFile: this);
            Medium = medium;
        }

        public static bool IsValidFile (string fullPath)
        {
            return Picture.IsValidFile (fullPath: fullPath) || Audio.IsValidFile (fullPath: fullPath) || Video.IsValidFile (fullPath: fullPath);
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


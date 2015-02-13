using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Util;
using Shell.Media.Content;
using Shell.Media.Database;
using Shell.Media.Files;
using SQLite;

namespace Shell.Media.Files
{
    public abstract class MediaFile : ValueObject<MediaFile>, IDatabaseAware
    {
        public static Type[] TYPES = new Type[] {
            typeof(Picture),
            typeof(Audio),
            typeof(Video),
            typeof(Document)
        };

        protected static MediaFileLibrary libMediaFile = new MediaFileLibrary ();

        [PrimaryKey, AutoIncrement]
        public int SqlId { get; set; }

        [Indexed]
        public string SqlHash { get { return Hash.Hash; } set { Hash = new HexString { Hash = value }; } }

        [Indexed]
        public int SqlAlbumId { get; set; }

        [Unique]
        public string RelativePath { get; set; }

        public string MimeType { get; protected set; }

        public bool IsDeleted { get; set; }

        public string FullPath { get { return Path.Combine (Database.RootDirectory, RelativePath); } }

        [Ignore]
        public abstract bool IsCompletelyIndexed { get; }

        [Ignore]
        public abstract DateTime? PreferredTimestampInternal { get; }

        [Ignore]
        public HexString Hash { get; set; }

        [Ignore]
        public string Filename { get { return Path.GetFileName (FullPath); } }

        [Ignore]
        public string Extension { get { return Path.GetExtension (Filename); } }

        [Ignore]
        public string AlbumPath { get { return MediaShareUtilities.GetAlbumPath (relativePath: RelativePath); } }

        [Ignore]
        public MediaDatabase Database { get; set; }

        //[Ignore]
        //public MediaDatabase Database { get; private set; }

        protected MediaFile (string fullPath, MediaDatabase database)
        {
            AssignDatabase (database);

            // set the full path
            RelativePath = MediaShareUtilities.GetRelativePath (fullPath: fullPath, rootDirectoryHolder: database);

            // compute the file's hash
            Hash = FileSystemUtilities.HashOfFile (path: fullPath);
        }

        protected MediaFile ()
        {
            DatabaseAwareness.AutoAssignDatabase (this);
        }

        public void AssignAlbum (Album album)
        {
            SqlAlbumId = album.SqlId;
        }

        public void AssignDatabase (MediaDatabase database)
        {
            Database = database;
        }

        public static MediaFile Create (string fullPath, MediaDatabase database)
        {
            MediaFile createdMedium;
            if (Picture.IsValidFile (fullPath: fullPath)) {
                createdMedium = new Picture (fullPath: fullPath, database: database);
            } else if (Video.IsValidFile (fullPath: fullPath)) {
                createdMedium = new Video (fullPath: fullPath, database: database);
            } else if (Audio.IsValidFile (fullPath: fullPath)) {
                createdMedium = new Audio (fullPath: fullPath, database: database);
            } else if (Document.IsValidFile (fullPath: fullPath)) {
                createdMedium = new Document (fullPath: fullPath, database: database);
            } else {
                throw new ArgumentException ("[MediaFile] Unknown file: " + fullPath);
            }
            return createdMedium;
        }

        public static bool IsValidFile (string fullPath)
        {
            return Picture.IsValidFile (fullPath: fullPath) || Audio.IsValidFile (fullPath: fullPath) || Video.IsValidFile (fullPath: fullPath) || Document.IsValidFile (fullPath: fullPath);
        }

        public static bool IsIgnoredFile (string fullPath)
        {
            return Path.GetFileName (fullPath) == CommonShare<MediaShare>.CONFIG_FILENAME;
        }

        public abstract void Index ();

        public Dictionary<string, string> Serialize ()
        {
            Dictionary<string, string> dict = new Dictionary<string, string> ();

            // save the mime type
            dict ["file:MimeType"] = MimeType;

            SerializeInternal (dict: dict);

            return dict;
        }

        public void Deserialize (Dictionary<string, string> dict)
        {
            // load the mime type
            MimeType = dict.ContainsKey ("file:MimeType") ? dict ["file:MimeType"] : null;

            DeserializeInternal (dict: dict);
        }

        protected abstract void SerializeInternal (Dictionary<string, string> dict);

        protected abstract void DeserializeInternal (Dictionary<string, string> dict);

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

        public DateTime PreferredTimestamp {
            get {

                DateTime? contentDate = PreferredTimestampInternal;
                if (contentDate.HasValue) {
                    return contentDate.Value;
                }

                DateTime filesystemDate = File.GetCreationTime (path: FullPath);

                DateTime filenameDate;
                // if the file name has a date
                if (NamingUtilities.GetFileNameDate (fileName: Filename, date: out filenameDate)) {
                    // if the file name date has a time component
                    if (filenameDate.HasTimeComponent ()) {
                        return filenameDate;
                    }
                    // if there is no time component int the file name date, look if the file system date is similar
                    else if (filenameDate.Year == filesystemDate.Year) {
                        return filesystemDate;
                    }
                    // if it isn't, return the file name date
                    else {
                        return filenameDate;
                    }
                }
                // if the file name doesn't have a date, return the file system date
                else {
                    return filesystemDate;
                }
            }
        }


        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Hash };
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


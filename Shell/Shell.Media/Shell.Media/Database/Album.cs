using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Util;
using Shell.Media.Content;
using Shell.Media.Files;
using SQLite;

namespace Shell.Media.Database
{
    public class Album : Core.Common.ValueObject<Album>, IFilterable, IDatabaseAware
    {
        [PrimaryKey, AutoIncrement]
        public int SqlId { get; set; }

        [Unique]
        public string Path { get; set; }

        [Ignore]
        public TableQuery<Picture> PictureQuery { get { return FileQuery<Picture> (); } }

        [Ignore]
        public TableQuery<Audio> AudioQuery { get { return FileQuery<Audio> (); } }

        [Ignore]
        public TableQuery<Video> VideoQuery { get { return FileQuery<Video> (); } }

        [Ignore]
        public TableQuery<Document> DocumentQuery { get { return FileQuery<Document> (); } }

        [Ignore]
        public IEnumerable<MediaFile> AllFilesQuery {
            get {
                foreach (Picture file in PictureQuery)
                    yield return file;
                foreach (Audio file in AudioQuery)
                    yield return file;
                foreach (Video file in VideoQuery)
                    yield return file;
                foreach (Document file in DocumentQuery)
                    yield return file;
            }
        }


        public bool IsDeleted { get; set; }

        // TODO
        //public bool IsHighQuality { get; set; }
        public bool IsHighQuality { get { return true; } }

        [Ignore]
        public MediaDatabase Database { get; set; }

        public void AssignDatabase (MediaDatabase database)
        {
            Database = database;
        }

        public TableQuery<T> FileQuery<T> ()
            where T : MediaFile, new()
        {
            DatabaseAwareness.SetDatabase<T> (Database);
            return Database.DB.Table<T> ().Where (f => f.SqlAlbumId == SqlId && !f.IsDeleted);
        }


        /*
        public Album (string albumPath, MediaShare share)
        {
            AlbumPath = albumPath;
            Files = new List<MediaFile> ();
            IsHighQuality = share.HighQualityAlbums.Any (ap => ap == "*" || albumPath.ToLower ().Trim ('/', '\\').StartsWith (ap.ToLower ().Trim ('/', '\\')))
            || AlbumPath.StartsWith (share.SpecialAlbumPrefix + PhotoSyncUtilities.SPECIAL_ALBUM_AUTO_BACKUP);
        }
        */

        public Album ()
        {
        }

        public bool ContainsFile (MediaFile mediaFile)
        {
            if (mediaFile is Picture)
                return PictureQuery.Contains (mediaFile as Picture);
            else if (mediaFile is Audio)
                return AudioQuery.Contains (mediaFile as Audio);
            else if (mediaFile is Video)
                return VideoQuery.Contains (mediaFile as Video);
            else if (mediaFile is Document)
                return DocumentQuery.Contains (mediaFile as Document);
            return false;
        }

        public bool ContainsFile (Func<Picture, bool> search)
        {
            return PictureQuery.Where (file => search (file)).Any ();
        }

        public bool ContainsFile (Func<Audio, bool> search)
        {
            return AudioQuery.Where (file => search (file)).Any ();
        }

        public bool ContainsFile (Func<Video, bool> search)
        {
            return VideoQuery.Where (file => search (file)).Any ();
        }

        public bool ContainsFile (Func<Document, bool> search)
        {
            return DocumentQuery.Where (file => search (file)).Any ();
        }

        public bool ContainsFile (Func<MediaFile, bool> search)
        {
            return AllFilesQuery.Where (file => search (file)).Any ();
        }

        // the relative path is a unique index
        public bool GetFileByRelativePath (string relativePath, out MediaFile result)
        {
            result = null;
            if (result == null) {
                result = PictureQuery.Where (file => file.RelativePath == relativePath).FirstOrDefault ();
            }
            if (result == null) {
                result = AudioQuery.Where (file => file.RelativePath == relativePath).FirstOrDefault ();
            }
            if (result == null) {
                result = VideoQuery.Where (file => file.RelativePath == relativePath).FirstOrDefault ();
            }
            if (result == null) {
                result = DocumentQuery.Where (file => file.RelativePath == relativePath).FirstOrDefault ();
            }
            if (result == null)
                Log.Debug ("GetFileByRelativePath: relativePath=", relativePath, ", result=null");
            return result != null;
        }

        public int Count<T> ()
            where T : MediaFile
        {
            if (typeof(T) == typeof(Picture))
                return PictureQuery.Count ();
            else if (typeof(T) == typeof(Audio))
                return AudioQuery.Count ();
            else if (typeof(T) == typeof(Video))
                return VideoQuery.Count ();
            else if (typeof(T) == typeof(Document))
                return DocumentQuery.Count ();
            else
                return PictureQuery.Count () + AudioQuery.Count () + VideoQuery.Count () + DocumentQuery.Count ();
        }


        public string[] FilterKeys ()
        {
            return new [] { Path };
        }

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Path };
        }
    }
}

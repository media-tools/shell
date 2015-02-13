using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Util;
using Shell.Media.Content;
using Shell.Media.Files;
using SQLite;

namespace Shell.Media.Database
{
    public class MediaDatabase : IRootDirectory
    {
        public string RootDirectory { get; private set; }

        public Album[] Albums { get { return DB.Table<Album> ().AssignDatabase (database: this).ToArray (); } }

        public HashSet<HexString> KnownMediaHashes {
            get {
                string sql = string.Join (" UNION ALL ", MediaFile.TYPES.Select (type => "select SqlHash as 'Str' from " + type.Name));
                Log.Debug ("KnownMediaHashes: sql=", sql);
                return DB.Query<SingleString> (sql).Select (str => new HexString { Hash = str.Str }).ToHashSet ();
            }
        }

        public HashSet<string> KnownRelativeFilenames {
            get {
                string sql = "select RelativePath as 'Str' from " + typeof(MediaFile).Name;
                Log.Debug ("KnownRelativeFilenames: sql=", sql);
                return DB.Query<SingleString> (sql).Select (str => str.Str).ToArray ().ToHashSet ();
            }
        }

        public IEnumerable<MediaFile> Media { get { return DB.Table<Picture> ().Cast<MediaFile> ()
                    .Union (DB.Table<Audio> ().Cast<MediaFile> ())
                    .Union (DB.Table<Video> ().Cast<MediaFile> ())
                    .Union (DB.Table<Document> ().Cast<MediaFile> ()); } }

        // sqlite
        public SQLiteConnection DB { get; private set; }

        public MediaDatabase (string rootDirectory)
        {
            RootDirectory = rootDirectory;


            string dbPath = Path.Combine (rootDirectory, "control.sqlite");
            Log.Debug ("Load database: ", dbPath);
            DB = new SQLiteConnection (dbPath);
            DB.CreateTable<Album> ();
            DB.CreateTable<Picture> ();
            DB.CreateTable<Audio> ();
            DB.CreateTable<Video> ();
            DB.CreateTable<Document> ();
        }

        public int AlbumCount { get { return DB.Table<Album> ().Count (); } }

        public void SaveChanges ()
        {
            foreach (Type type in MediaFile.TYPES) {
                DB.Execute ("delete from " + type.Name + " where IsDeleted = 1");
            }
            foreach (Type type in MediaFile.TYPES) {
                // shouldn't be necessary!
                // DB.Execute ("delete from " + type.Name + " where SqlAlbumId is null");
            }
        }

        public Album GetAlbum (string albumPath)
        {
            Album result = DB.Table<Album> ().Where (a => a.Path == albumPath).FirstOrDefault ();
            if (result == null) {
                Album newAlbum = new Album () {
                    Path = albumPath,
                    Database = this,
                };
                DB.Insert (newAlbum);
                result = DB.Table<Album> ().Where (a => a.Path == albumPath).FirstOrDefault ();
                if (result == null) {
                    Log.Error ("Bug! MediaDatabase: Can't add album to database!");
                    return null;
                }
                Log.Debug ("MediaDatabase: New Album \"", result.Path, "\" = ", result.SqlId);
            }

            result.AssignDatabase (database: this);
            return result;
        }

        public void RemoveAlbum (Album album)
        {
            DB.Table<Album> ().Delete (a => a.Path == album.Path);

            Debug.Assert (!DB.Table<Album> ().Any (a => a.Path == album.Path), "Album is deleted from the database, yet it is still in the database: " + album.Path);
        }

        public MediaFile GetMediumByHash (HexString hash)
        {
            Log.Debug ("GetMediumByHash (1): ", hash);
            MediaFile[] result = DB.Table<Picture> ().Where (m => m.SqlHash == hash.Hash).ToArray ();
            if (result.Length != 0) {
                return result [0];
            }

            Log.Debug ("GetMediumByHash (2): ", hash);
            result = DB.Table<Audio> ().Where (m => m.SqlHash == hash.Hash).ToArray ();
            if (result.Length != 0) {
                return result [0];
            }

            Log.Debug ("GetMediumByHash (3): ", hash);
            result = DB.Table<Document> ().Where (m => m.SqlHash == hash.Hash).ToArray ();
            if (result.Length != 0) {
                return result [0];
            }

            Log.Debug ("GetMediumByHash (4): ", hash);
            result = DB.Table<Video> ().Where (m => m.SqlHash == hash.Hash).ToArray ();
            if (result.Length != 0) {
                return result [0];
            }

            return null;
        }

        public bool GetMediumByHash (HexString hash, out MediaFile medium)
        {
            Log.Debug ("GetMediumByHash (wrapper): ", hash);

            medium = GetMediumByHash (hash: hash);
            Log.Debug ("GetMediumByHash (wrapper):result= ", medium);
            return medium != null;
        }


        public void UpdateAlbum (Album album)
        {
            if (album != null) {
                DB.Update (album);
            } else {
                throw new ArgumentNullException (string.Format ("Album is null: {0}", album));
            }
        }

        public void InsertFile (MediaFile mediaFile)
        {
            if (mediaFile != null) {
                if (mediaFile is Picture)
                    DB.Insert (mediaFile, typeof(Picture)); //, "OR REPLACE"
                else if (mediaFile is Audio)
                    DB.Insert (mediaFile, typeof(Audio));
                else if (mediaFile is Document)
                    DB.Insert (mediaFile, typeof(Document));
                else if (mediaFile is Video)
                    DB.Insert (mediaFile, typeof(Video));

            } else {
                throw new ArgumentNullException (string.Format ("MediaFile is null: {0}", mediaFile));
            }
        }

        public void RemoveFile (MediaFile mediaFile)
        {
            if (mediaFile != null) {
                DB.Delete (mediaFile);
            } else {
                throw new ArgumentNullException (string.Format ("MediaFile is null: {0}", mediaFile));
            }
        }

        public void UpdateFile (MediaFile mediaFile)
        {
            if (mediaFile != null) {
                DB.Update (mediaFile);
            } else {
                throw new ArgumentNullException (string.Format ("MediaFile is null: {0}", mediaFile));
            }
        }
    }

    class SingleString
    {
        public string Str { get; set; }
    }
}


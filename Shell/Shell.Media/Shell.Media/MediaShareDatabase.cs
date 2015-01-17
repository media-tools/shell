using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.Shares;
using Shell.Common.Util;
using Shell.Media.Content;

namespace Shell.Media
{
    public class MediaShareDatabase : IRootDirectory
    {
        public string RootDirectory { get; private set; }

        public Dictionary<string, Album> AlbumMap { get; private set; }

        public Album[] Albums { get { return AlbumMap.Values.ToArray (); } }

        public Dictionary<HexString, Medium> MediaMap { get; private set; }

        public Medium[] Media { get { return MediaMap.Values.ToArray (); } }

        private Func<string, Album> CreateAlbum;

        public MediaShareDatabase (string rootDirectory, Func<string, Album> createAlbum)
        {
            RootDirectory = rootDirectory;
            CreateAlbum = createAlbum;
            AlbumMap = new Dictionary<string, Album> ();
            MediaMap = new Dictionary<HexString, Medium> ();
        }

        public int AlbumCount { get { return AlbumMap.Count; } }

        public int MediumCount { get { return MediaMap.Count; } }

        public void Clear ()
        {
            MediaMap.Clear ();
            AlbumMap.Clear ();
        }

        public Album GetAlbum (string albumPath)
        {
            return AlbumMap.TryCreateEntry (key: albumPath, defaultValue: () => CreateAlbum (albumPath));
        }

        public void RemoveAlbum (Album album)
        {
            AlbumMap.Remove (album.AlbumPath);
        }

        public bool GetMediumByHash (HexString hash, out Medium medium)
        {
            if (MediaMap.ContainsKey (hash)) {
                medium = MediaMap [hash];
                return true;
            } else {
                medium = null;
                return false;
            }
        }



        public void AddAlbum (Album album)
        {
            if (album != null) {
                AlbumMap [album.AlbumPath] = album;
            } else {
                throw new ArgumentNullException (string.Format ("Album is null: {0}", album));
            }
        }

        public void AddMedium (Medium medium)
        {
            if (medium != null) {
                MediaMap [medium.Hash] = medium;
            } else {
                throw new ArgumentNullException (string.Format ("Media is null: {0}", medium));
            }
        }
    }
}


using System;
using Shell.Pictures;
using System.Text.RegularExpressions;

namespace Shell.GoogleSync.Photos
{
    public static class PhotoSyncUtilities
    {
        public static string SYNCED_ALBUM_PREFIX = "[";
        public static string SYNCED_ALBUM_SUFFIX = "]";

        public static bool IsSyncedAlbum (WebAlbum album)
        {
            return album.Title.StartsWith (SYNCED_ALBUM_PREFIX) && album.Title.EndsWith (SYNCED_ALBUM_SUFFIX);
        }

        public static string ToSyncedAlbumName (Album album)
        {
            return SYNCED_ALBUM_PREFIX + album.AlbumPath + SYNCED_ALBUM_SUFFIX;
        }

        public static bool IsIncludedInSync (Album album)
        {
            string name = album.AlbumPath;
            return name.Length > 0 && !Regex.IsMatch (name, "[0-9]{4}[/-][0-9]{2}[/-][0-9]{2}");
        }
    }
}


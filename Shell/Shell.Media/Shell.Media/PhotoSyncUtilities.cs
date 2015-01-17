using System;
using Shell.Media;
using System.Text.RegularExpressions;

namespace Shell.Media
{
    public static class PhotoSyncUtilities
    {
        public static string SYNCED_ALBUM_PREFIX = "[";
        public static string SYNCED_ALBUM_SUFFIX = "]";

        public static bool IsSyncedAlbum (IWebAlbum album)
        {
            return album.Title.StartsWith (SYNCED_ALBUM_PREFIX) && album.Title.EndsWith (SYNCED_ALBUM_SUFFIX);
        }

        public static string SPECIAL_ALBUM_AUTO_BACKUP = "Auto Backup";
        public static string SPECIAL_ALBUM_HANGOUT = "Hangout:";

        public static string ToSyncedAlbumName (Album album)
        {
            return SYNCED_ALBUM_PREFIX + album.AlbumPath + SYNCED_ALBUM_SUFFIX;
        }

        public static bool IsIncludedInSync (Album album)
        {
            string name = album.AlbumPath;
            return name.Length > 0 && !Regex.IsMatch (name, "(^|/)[0-9]{4}[/-][0-9]{2}[/-][0-9]{2}");
        }
    }
}


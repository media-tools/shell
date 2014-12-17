using System;
using System.Collections.Generic;
using Shell.Pictures.Files;
using System.Linq;
using Shell.Common.IO;

namespace Shell.Pictures
{
    public class Album
    {
        public string AlbumPath { get; private set; }

        public List<MediaFile> Files { get; private set; }

        public bool IsDeleted { get; set; }

        public bool IsHighQuality { get; set; }

        public Album (string albumPath, PictureShare share)
        {
            AlbumPath = albumPath;
            Files = new List<MediaFile> ();
            IsHighQuality = share.HighQualityAlbums.Any (ap => ap == "*" || albumPath.ToLower ().Trim ('/', '\\').StartsWith (ap.ToLower ().Trim ('/', '\\')));
        }

        public void AddFile (MediaFile mediaFile)
        {
            Files.Add (mediaFile);
        }

        public void RemoveFile (MediaFile mediaFile)
        {
            Files.Remove (mediaFile);
        }

        public bool ContainsFile (MediaFile mediaFile)
        {
            return Files.Contains (mediaFile);
        }

        public bool ContainsFile (Func<MediaFile, bool> search)
        {
            //foreach (MediaFile file in Files)
            //	Log.Debug ("in album: ", file.FullPath);
            return Files.Where (file => search (file)).Any ();
        }

        public bool GetFile (Func<MediaFile, bool> search, out MediaFile result)
        {
            IEnumerable<MediaFile> found = Files.Where (file => search (file));
            if (found.Any ()) {
                result = found.First ();
                return true;
            } else {
                result = null;
                return false;
            }
        }
    }
}

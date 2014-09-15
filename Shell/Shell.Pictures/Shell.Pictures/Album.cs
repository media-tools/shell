using System;
using System.Collections.Generic;
using Shell.Pictures.Files;
using System.Linq;

namespace Shell.Pictures
{
    public class Album
    {
        public string AlbumPath { get; private set; }

        public List<MediaFile> Files { get; private set; }

        public Album (string albumPath)
        {
            AlbumPath = albumPath;
            Files = new List<MediaFile> ();
        }

        public void Add (MediaFile mediaFile)
        {
            Files.Add (mediaFile);
        }

        public bool Contains (MediaFile mediaFile)
        {
            return Files.Contains (mediaFile);
        }

        public bool Contains (Func<MediaFile, bool> search)
        {
            return Files.Where (file => search (file)).Any ();
        }

        public bool Get (Func<MediaFile, bool> search, out MediaFile result)
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


using System;
using System.Collections.Generic;

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
    }
}


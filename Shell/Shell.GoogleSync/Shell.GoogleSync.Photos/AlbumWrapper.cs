using System;
using Google.GData.Photos;
using Google.Picasa;

namespace Shell.GoogleSync
{
    public class AlbumWrapper
    {
        public string Title { get; private set; }

        public string Id { get; private set; }

        public Album Accessor { get; private set; }

        public uint NumPhotos { get; private set; }

        public AlbumWrapper (Album accessor)
        {
            Accessor = accessor;
            Update ();
        }

        private void Update ()
        {
            NumPhotos = Accessor.NumPhotos;
            Title = Accessor.Title;
            Id = Accessor.Id;
        }
    }
}


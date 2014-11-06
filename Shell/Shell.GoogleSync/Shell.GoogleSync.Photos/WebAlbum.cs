using System;
using Google.GData.Photos;
using Google.Picasa;
using Shell.Common.IO;

namespace Shell.GoogleSync.Photos
{
    public class WebAlbum
    {
        public AlbumCollection AlbumCollection { get; private set; }

        public Album InternalAlbum { get; private set; }

        public string Title { get; private set; }

        public string Id { get; private set; }

        public uint NumPhotos { get; private set; }

        public WebAlbum (AlbumCollection albumCollection, Album internalAlbum)
        {
            AlbumCollection = albumCollection;
            InternalAlbum = internalAlbum;
            Update ();
        }

        private void Update ()
        {
            NumPhotos = InternalAlbum.NumPhotos;
            Title = InternalAlbum.Title;
            Id = InternalAlbum.Id;
        }

        public void PrintPhotos ()
        {
            WebPhoto[] albums = AlbumCollection.GetPhotos (album: this);

            Log.Message (albums.ToStringTable (
                p => LogColor.Reset,
                new[] { "Title", "Id", "Dimensions" },
                p => p.Title,
                p => p.Id,
                p => p.Dimensions.Width + "x" + p.Dimensions.Height
            ));
        }

        public void Delete ()
        {
            Log.Message ("Delete album.");
            AlbumCollection.CatchErrors (() => {
                InternalAlbum.AtomEntry.Delete ();
            });
        }
    }
}


using System;
using Google.GData.Photos;
using Google.Picasa;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.GoogleSync.Photos
{
    public class WebAlbum : Shell.Media.IWebAlbum, IFilterable
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
            WebPhotoCollection result = AlbumCollection.GetPhotos (album: this, deleteDuplicates: false, holdInternals: false);

            Log.Message (result.WebFiles.ToStringTable (
                p => LogColor.Reset,
                new[] { "Filename", "Id", "Dimensions" },
                p => p.Filename,
                p => p.Id,
                p => p.Dimensions.Width + "x" + p.Dimensions.Height
            ));
        }

        public void Delete (bool verbose = true)
        {
            if (verbose) {
                Log.Message ("Delete album: " + Title);
            }
            AlbumCollection.CatchErrors (() => {
                InternalAlbum.AtomEntry.Delete ();
            });
        }

        public string[] FilterKeys ()
        {
            return new [] { Title };
        }
    }
}


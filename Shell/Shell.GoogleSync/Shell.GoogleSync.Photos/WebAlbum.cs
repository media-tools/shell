using System;
using Google.GData.Photos;
using Google.Picasa;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Collections.Generic;

namespace Shell.GoogleSync.Photos
{
    public class WebAlbum : ValueObject<WebAlbum>, Shell.Media.Web.IWebAlbum, IFilterable
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
                new[] { "Filename for Download", "Filename", "Id", "Dimensions", "Timestamp" },
                p => p.FilenameForDownload,
                p => p.Filename,
                p => p.Id,
                p => p.Dimensions.Width + "x" + p.Dimensions.Height,
                p => p.Timestamp.ToString ("yyyy-MM-dd HH:mm:ss")
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

        public override string ToString ()
        {
            return string.Format ("[WebAlbum: Title={0}, Id={1}, NumPhotos={2}]", Title, Id, NumPhotos);
        }

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Id };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<WebAlbum>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (WebAlbum a, WebAlbum b)
        {
            return ValueObject<WebAlbum>.Equality (a, b);
        }

        public static bool operator != (WebAlbum a, WebAlbum b)
        {
            return ValueObject<WebAlbum>.Inequality (a, b);
        }
    }
}


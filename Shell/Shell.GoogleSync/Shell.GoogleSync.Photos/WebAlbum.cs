using System;
using System.Collections.Generic;
using Google.GData.Photos;
using Google.Picasa;
using Newtonsoft.Json;
using Shell.Common.IO;
using Shell.Common.Util;
using Core.Math;
using System.Linq;
using Core.Common;

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

            Shell.Common.IO.Log.Info (result.WebFiles.ToStringTable (
                p => LogColor.Reset,
                new[] { "Filename for Download", "Filename", "Id", "Dimensions", "Timestamp", "Download URL" },
                p => p.FilenameForDownload,
                p => p.Filename,
                p => p.Id,
                p => p.Dimensions.Width + "x" + p.Dimensions.Height,
                p => p.Timestamp.ToString ("yyyy-MM-dd HH:mm:ss"),
                p => p.DownloadUrl
            ));
        }

        public JsonPhoto[] JsonPhotos ()
        {
            WebPhotoCollection result = AlbumCollection.GetPhotos (album: this, deleteDuplicates: false, holdInternals: false);

            return result.WebFiles.Select (
                p => new JsonPhoto {
                    GoogleId = p.Id,
                    Filenames = new string[] { p.Filename, p.FilenameForDownload },
                    Width = p.Dimensions.Width,
                    Height = p.Dimensions.Height,
                    HostedURL = p.DownloadUrl
                }
            ).ToArray ();
        }

        public class JsonPhoto
        {
            [JsonProperty ("google_photos_id")]
            public string GoogleId { get; set; } = "";

            [JsonProperty ("filenames")]
            public string[] Filenames { get; set; } = new string[0];

            [JsonProperty ("width")]
            public int Width { get; set; } = 0;

            [JsonProperty ("height")]
            public int Height { get; set; } = 0;

            [JsonProperty ("url_hosted")]
            public string HostedURL { get; set; } = null;
        }

        public void Delete (bool verbose = true)
        {
            if (verbose) {
                Shell.Common.IO.Log.Info ("Delete album: " + Title);
            }
            AlbumCollection.CatchErrors (() => InternalAlbum.AtomEntry.Delete ());
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


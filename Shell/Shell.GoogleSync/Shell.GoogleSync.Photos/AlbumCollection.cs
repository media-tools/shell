using System;
using Google.GData.Client;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;
using Google.GData.Photos;
using System.Collections.Generic;
using Google.Picasa;

namespace Shell.GoogleSync
{
    public class AlbumCollection : GDataLibrary
    {
        private PicasaService service;

        public AlbumCollection (GoogleAccount account)
            : base (account)
        {
            service = new PicasaService (requestFactory.ApplicationName);
            service.RequestFactory = requestFactory;
        }

        private AlbumCollection ()
            : base ()
        {
        }

        public WebAlbum[] GetAlbums ()
        {
            List<WebAlbum> albumList = new List<WebAlbum> ();

            CatchErrors (() => {
                AlbumQuery query = new AlbumQuery (PicasaQuery.CreatePicasaUri (account.Id));
                PicasaFeed feed = service.Query (query);

                foreach (PicasaEntry entry in feed.Entries) {
                    Album internalAlbum = new Album ();
                    internalAlbum.AtomEntry = entry;
                    WebAlbum album = new WebAlbum (albumCollection: this, internalAlbum: internalAlbum);
                    albumList.Add (album);
                }
            });

            return albumList.ToArray ();
        }

        public void PrintAlbums ()
        {
            WebAlbum[] albums = GetAlbums ();

            Log.Message (albums.ToStringTable (
                a => LogColor.Reset,
                new[] { "Title", "Id", "NumPhotos" },
                a => a.Title,
                a => a.Id,
                a => a.NumPhotos
            ));
        }

        public WebPhoto[] GetPhotos (WebAlbum album)
        {
            List<WebPhoto> albumList = new List<WebPhoto> ();

            CatchErrors (() => {
                PhotoQuery query = new PhotoQuery (PicasaQuery.CreatePicasaUri (account.Id, album.Id));
                PicasaFeed feed = service.Query (query);

                foreach (PicasaEntry entry in feed.Entries) {
                    Photo internalPhoto = new Photo ();
                    internalPhoto.AtomEntry = entry;
                    WebPhoto photo = new WebPhoto (albumCollection: this, album: album, internalPhoto: internalPhoto);
                    albumList.Add (photo);
                }
            });

            return albumList.ToArray ();
        }

    }
}


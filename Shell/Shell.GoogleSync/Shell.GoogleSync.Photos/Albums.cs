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
    public class Albums : GDataLibrary
    {
        private PicasaService service;

        public Albums (GoogleAccount account)
            : base (account)
        {
            service = new PicasaService (requestFactory.ApplicationName);
            service.RequestFactory = requestFactory;
        }

        private Albums ()
            : base ()
        {
        }

        public void PrintAllAlbums ()
        {
            CatchErrors (() => {
                AlbumQuery query = new AlbumQuery (PicasaQuery.CreatePicasaUri (account.Id));
                PicasaFeed feed = service.Query (query);

                List<AlbumWrapper> albumList = new List<AlbumWrapper> ();
                foreach (PicasaEntry entry in feed.Entries) {
                    Album accessor = new Album ();
                    accessor.AtomEntry = entry;
                    AlbumWrapper album = new AlbumWrapper (accessor: accessor);
                    albumList.Add (album);
                }


                Log.Message (albumList.ToStringTable (
                    a => LogColor.Reset,
                    new[] { "Title", "Id", "NumPhotos" },
                    a => a.Title,
                    a => a.Id,
                    a => a.NumPhotos
                ));
            });
        }
    }
}


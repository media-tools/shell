using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Pictures;
using Shell.Pictures.Content;
using Shell.Pictures.Files;

namespace Shell.GoogleSync.Photos
{
    public class AlbumSyncStatus
    {
        public Album LocalAlbum;
        public MediaFile[] LocalPhotos;
        public WebAlbum WebAlbum;
        public WebPhoto[] WebPhotos;

        public MediaFile[] OnlyInLocalAlbum;
        public WebPhoto[] OnlyInWebAlbum;

        public Type[] ValidTypes;

        public AlbumSyncStatus (Album localAlbum, WebAlbum webAlbum, WebPhoto[] webPhotos, Type[] validTypes)
        {
            ValidTypes = validTypes;

            LocalAlbum = localAlbum;
            WebAlbum = webAlbum;
            WebPhotos = webPhotos.OrderBy (f => f.Title).ToArray ();

            // filter all photos from the list of local files
            LocalPhotos = LocalAlbum.Files.Where (f => validTypes.Any (type => type.IsAssignableFrom (f.Medium.GetType ()))).OrderBy (f => f.Name).ToArray ();

            Compare ();
        }

        private void Compare ()
        {
            // find files that only exist locally
            List<MediaFile> onlyLocal = new List<MediaFile> ();
            foreach (MediaFile localFile in LocalPhotos) {
                if (!WebPhotos.Any (wp => wp.Title == localFile.Name)) {
                    onlyLocal.Add (localFile);
                }
            }
            OnlyInLocalAlbum = onlyLocal.ToArray ();

            // find files that only exist in the web album
            List<WebPhoto> onlyWeb = new List<WebPhoto> ();
            foreach (WebPhoto webFile in WebPhotos) {
                if (!LocalPhotos.Any (lf => lf.Name == webFile.Title)) {
                    onlyWeb.Add (webFile);
                }
            }
            OnlyInWebAlbum = onlyWeb.ToArray ();
        }
    }
}


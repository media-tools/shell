using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Media;
using Shell.Media.Content;
using Shell.Media.Files;

namespace Shell.GoogleSync.Photos
{
    public class AlbumSyncStatus
    {
        public Album LocalAlbum;
        public MediaFile[] LocalFiles;
        public MediaFile[] LocalPhotos;
        public MediaFile[] LocalVideos;
        public WebAlbum WebAlbum;
        public WebPhoto[] WebFiles;

        public MediaFile[] FilesOnlyInLocalAlbum;
        public WebPhoto[] FilesOnlyInWebAlbum;

        public AlbumSyncStatus (Album localAlbum, WebAlbum webAlbum, WebPhoto[] webFiles)
        {
            LocalAlbum = localAlbum;
            WebAlbum = webAlbum;
            WebFiles = webFiles.OrderBy (f => f.Title).ToArray ();

            // filter all photos from the list of local files
            LocalPhotos = LocalAlbum.Files.Where (f => f.Medium is Picture).OrderBy (f => f.Name).ToArray ();
            LocalVideos = LocalAlbum.Files.Where (f => f.Medium is Video).OrderBy (f => f.Name).ToArray ();
            LocalFiles = LocalPhotos.Concat (LocalVideos).ToArray ();

            Compare ();
        }

        private void Compare ()
        {
            // find files that only exist locally
            List<MediaFile> onlyLocal = new List<MediaFile> ();
            foreach (MediaFile localFile in LocalFiles) {
                if (!WebFiles.Any (wp => wp.Filename == localFile.Name)) {
                    onlyLocal.Add (localFile);
                }
            }
            FilesOnlyInLocalAlbum = onlyLocal.OrderBy (f => f.Name).ToArray ();

            // find files that only exist in the web album
            List<WebPhoto> onlyWeb = new List<WebPhoto> ();
            foreach (WebPhoto webFile in WebFiles) {
                if (!LocalFiles.Any (lf => lf.Name == webFile.Filename)) {
                    onlyWeb.Add (webFile);
                }
            }
            FilesOnlyInWebAlbum = onlyWeb.OrderBy (f => f.Title).ToArray ();
        }
    }
}


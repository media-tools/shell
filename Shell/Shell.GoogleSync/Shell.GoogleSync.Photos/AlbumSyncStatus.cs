using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Media;
using Shell.Media.Content;
using Shell.Media.Files;
using Shell.Media.Database;
using Shell.Common.IO;

namespace Shell.GoogleSync.Photos
{
    public class AlbumSyncStatus
    {
        public Album LocalAlbum;
        public MediaFile[] LocalFiles;
        public Picture[] LocalPhotos;
        public Video[] LocalVideos;
        public WebAlbum WebAlbum;
        public WebPhoto[] WebFiles;

        public MediaFile[] FilesOnlyInLocalAlbum;
        public WebPhoto[] FilesOnlyInWebAlbum;

        public AlbumSyncStatus (Album localAlbum, WebAlbum webAlbum, WebPhoto[] webFiles, bool requireStrictFilenames, bool acceptDifferentExtensions)
        {
            //Log.Debug ("a");

            LocalAlbum = localAlbum;
            WebAlbum = webAlbum;
            WebFiles = webFiles.OrderBy (f => f.Filename).ToArray ();

            // filter all photos from the list of local files
            LocalPhotos = LocalAlbum.PictureQuery.OrderBy (f => f.RelativePath).ToArray ();
            // must be RelativePath instead of FileName because FileName is no SQL column!!!
            LocalVideos = LocalAlbum.VideoQuery.OrderBy (f => f.RelativePath).ToArray ();
            LocalFiles = LocalPhotos.Cast<MediaFile> ().Concat (LocalVideos.Cast<MediaFile> ()).ToArray ();

            //Log.Debug ("b");

            // find files that only exist locally
            List<MediaFile> onlyLocal = new List<MediaFile> ();
            foreach (MediaFile localFile in LocalFiles) {
                if (WebFiles.Any (wp => 

                    (wp.FilenameForDownload == localFile.Filename)
                    || (!requireStrictFilenames && wp.Filename == localFile.Filename)
                    || (acceptDifferentExtensions
                    && wp.FilenameForDownloadWithoutExtension == localFile.FilenameWithoutExtension)
                
                    )) {
                    continue;
                }

                onlyLocal.Add (localFile);
            }
            FilesOnlyInLocalAlbum = onlyLocal.OrderBy (f => f.Filename).ToArray ();
            
            //Log.Debug ("c");

            // find files that only exist in the web album
            List<WebPhoto> onlyWeb = new List<WebPhoto> ();
            foreach (WebPhoto webFile in WebFiles) {
                if (LocalFiles.Any (lf => 

                    (lf.Filename == webFile.FilenameForDownload)
                    || (!requireStrictFilenames && lf.Filename == webFile.Filename)
                    || (acceptDifferentExtensions
                    && lf.FilenameWithoutExtension == webFile.FilenameForDownloadWithoutExtension)
                
                    )) {
                    continue;
                }

                onlyWeb.Add (webFile);
            }
            FilesOnlyInWebAlbum = onlyWeb.OrderBy (f => f.Filename).ToArray ();

            //Log.Debug ("d");
        }
    }
}


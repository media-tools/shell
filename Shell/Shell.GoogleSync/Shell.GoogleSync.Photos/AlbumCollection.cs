using System;
using Google.GData.Client;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;
using Google.GData.Photos;
using System.Collections.Generic;
using Picasa = Google.Picasa;
using Shell.Pictures;
using System.Linq;
using Shell.Pictures.Files;

namespace Shell.GoogleSync.Photos
{
    public class AlbumCollection : GDataLibrary
    {
        private PicasaService service;
        private static MediaFileLibrary libMediaFile = new MediaFileLibrary ();

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
                    Picasa.Album internalAlbum = new Picasa.Album ();
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
                    Picasa.Photo internalPhoto = new Picasa.Photo ();
                    internalPhoto.AtomEntry = entry;
                    WebPhoto photo = new WebPhoto (albumCollection: this, album: album, internalPhoto: internalPhoto);
                    albumList.Add (photo);
                }
            });

            return albumList.ToArray ();
        }

        private void CreateMissingWebAlbums (PictureShare share)
        {
            if (share.Albums.Count > 0) {
                // list local albums
                Log.Message ("List local albums:");
                Log.Indent++;
                foreach (Album localAlbum in share.Albums) {
                    if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {
                        Log.Message (" - ", PhotoSyncUtilities.ToSyncedAlbumName (localAlbum));
                    }
                }
                Log.Indent--;

                WebAlbum[] webAlbums = GetAlbums ();
                // create missing web albums
                Log.Message ("Create non-existant web albums:");
                Log.Indent++;
                foreach (Album localAlbum in share.Albums) {
                    if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {
                        string webAlbumName = PhotoSyncUtilities.ToSyncedAlbumName (localAlbum);

                        if (!webAlbums.Any (wa => wa.Title == webAlbumName)) {
                            Log.Message ("Webalbum does not exist: ", webAlbumName);
                            Log.Indent++;
                            CatchErrors (() => {
                                AlbumEntry entry = new AlbumEntry ();
                                entry.Title.Text = webAlbumName;
                                entry.Summary.Text = webAlbumName;

                                Picasa.Album internalAlbum = new Picasa.Album ();
                                internalAlbum.AtomEntry = entry;
                                internalAlbum.Access = "private";

                                Uri query = new Uri (PicasaQuery.CreatePicasaUri (account.Id));
                                PicasaEntry createdEntry = (PicasaEntry)service.Insert (query, entry);
                                Log.Message ("Created entry: ", createdEntry.Id);
                            });
                            Log.Indent--;
                        }
                    }
                }
                Log.Indent--;
            } else {
                Log.Message ("There are no local albums!");
            }
        }

        private Dictionary<WebAlbum, WebPhoto[]> ListWebAlbums (PictureShare share)
        {
            // list photos in web albums
            Log.Message ("List photos in web albums: ");
            Log.Indent++;
            Dictionary<WebAlbum, WebPhoto[]> webAlbums = new Dictionary<WebAlbum, WebPhoto[]> ();
            foreach (WebAlbum album in GetAlbums()) {
                Log.Message ("- ", album.Title);
                if (PhotoSyncUtilities.IsSyncedAlbum (album)) {
                    webAlbums [album] = GetPhotos (album);
                }
            }
            Log.Indent--;
            return webAlbums;
        }

        private AlbumSyncStatus[] CompareLocalAndWebAlbums (PictureShare share, Dictionary<WebAlbum, WebPhoto[]> webAlbums)
        {
            // compare local and web albums...
            Log.Message ("Compare local and web albums:");
            Log.Indent++;

            List<AlbumSyncStatus> syncStatusList = new List<AlbumSyncStatus> ();
            foreach (Album localAlbum in share.Albums) {
                if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {
                    string webAlbumName = PhotoSyncUtilities.ToSyncedAlbumName (localAlbum);

                    if (webAlbums.Keys.Any (wa => wa.Title == webAlbumName)) {
                        WebAlbum webAlbum = webAlbums.Keys.First (wa => wa.Title == webAlbumName);
                        WebPhoto[] webPhotos = webAlbums [webAlbum];

                        AlbumSyncStatus syncStatus = new AlbumSyncStatus (localAlbum: localAlbum, webAlbum: webAlbum, webPhotos: webPhotos);
                        syncStatusList.Add (syncStatus);
                    }
                }
            }
            Log.Indent--;

            return syncStatusList.ToArray ();
        }

        private void PrintSyncStatus (AlbumSyncStatus[] syncStatusList)
        {
            // print the differences between local and web albums
            Log.Message ("Differences between local and web albums: ");
            Log.Indent++;
            Log.Message (syncStatusList.ToStringTable (
                s => LogColor.Reset,
                new[] { "Album", "Local (total)", "Web (total)", "Local (only)", "Web (only)" },
                s => s.WebAlbum.Title,
                s => s.LocalPhotos.Length,
                s => s.WebPhotos.Length,
                s => s.OnlyInLocalAlbum.Length,
                s => s.OnlyInWebAlbum.Length
            ));
            Log.Indent--;
        }

        private void UploadDifferences (AlbumSyncStatus[] syncStatusList)
        {
            Log.Message ("Upload: ");
            Log.Indent++;
            foreach (AlbumSyncStatus syncStatus in syncStatusList) {
                if (syncStatus.OnlyInLocalAlbum.Length > 0) {
                    Log.Message ("Album: ", syncStatus.WebAlbum.Title);
                    Log.Indent++;
                    fs.Runtime.ClearTempFiles ();
                    foreach (MediaFile localPhoto in syncStatus.OnlyInLocalAlbum) {
                        try {
                            CatchErrors (() => {
                                string fullPath = localPhoto.FullPath;
                                string mimeType = libMediaFile.GetMimeTypeByExtension (fullPath: localPhoto.FullPath);
                                if (mimeType != null) {
                                    if (mimeType == "image/jpeg" || mimeType == "image/png") {
                                        Log.Message ("Resize File: [", mimeType, "] ", localPhoto.Name);
                                        string tempPath = fs.Runtime.GetTempFilename (fullPath);
                                        if (ImageResizeUtilities.ResizeImage (sourcePath: fullPath, destPath: tempPath, mimeType: mimeType, maxHeight: 2048, maxWidth: 2048)) {
                                            fullPath = tempPath;
                                        }
                                    }
                                    Log.Message ("Upload File: [", mimeType, "] ", localPhoto.Name);
                                    Uri postUri = new Uri (PicasaQuery.CreatePicasaUri (account.Id, syncStatus.WebAlbum.Id));

                                    System.IO.FileInfo fileInfo = new System.IO.FileInfo (fullPath);
                                    System.IO.FileStream fileStream = fileInfo.OpenRead ();

                                    PicasaEntry entry = (PicasaEntry)service.Insert (postUri, fileStream, mimeType, localPhoto.Name);

                                    fileStream.Close ();
                                } else {
                                    Log.Error ("Unknown mime type: ", localPhoto.FullPath);
                                }
                            });
                        } catch (Exception ex) {
                            Log.Error (ex);
                        }
                    }
                    fs.Runtime.ClearTempFiles ();
                    Log.Indent--;
                }
            }
            Log.Indent--;
        }

        public void UploadShare (PictureShare share)
        {
            // create missing web albums
            CreateMissingWebAlbums (share: share);

            // list photos in web albums
            Dictionary<WebAlbum, WebPhoto[]> webAlbums = ListWebAlbums (share: share);

            // compare local and web albums...
            AlbumSyncStatus[] syncStatusList = CompareLocalAndWebAlbums (share: share, webAlbums: webAlbums);

            // print the differences between local and web albums
            PrintSyncStatus (syncStatusList: syncStatusList);

            // upload missing photos
            UploadDifferences (syncStatusList: syncStatusList);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using Google.GData.Client;
using Google.GData.Photos;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;
using Shell.Pictures;
using Shell.Pictures.Content;
using Shell.Pictures.Files;
using Picasa = Google.Picasa;
using System.Threading.Tasks;

namespace Shell.GoogleSync.Photos
{
    public class AlbumCollection : GDataLibrary
    {
        private PicasaService service;
        private static MediaFileLibrary libMediaFile = new MediaFileLibrary ();

        public AlbumCollection (GoogleAccount account)
            : base (account)
        {
        }

        protected override void UpdateAuth ()
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

        public WebPhotoCollection GetPhotos (WebAlbum album)
        {
            HashSet<string> uniqueFilenames = new HashSet<string> ();
            WebPhotoCollection result = new WebPhotoCollection ();

            string errorMessage;
            CatchErrors (todo: () => {
                int startIndex = 1; // starts with 1
                int numResults = 0;
                do {
                    startIndex += numResults;
                    numResults = 0;

                    PhotoQuery query = new PhotoQuery (PicasaQuery.CreatePicasaUri (account.Id, album.Id));
                    query.NumberToRetrieve = 1000;
                    query.StartIndex = startIndex;
                    PicasaFeed feed = service.Query (query);

                    foreach (PicasaEntry entry in feed.Entries) {
                        Picasa.Photo internalPhoto = new Picasa.Photo ();
                        internalPhoto.AtomEntry = entry;
                        string uniqueFilename = internalPhoto.Title;
                        if (uniqueFilenames.Contains (uniqueFilename)) {
                            result.Log ("Delete duplicate: " + uniqueFilename);
                            CatchErrors (() => entry.Delete ());
                        } else {
                            WebPhoto photo = new WebPhoto (albumCollection: this, album: album, internalPhoto: internalPhoto);
                            result.AddWebFile (photo);
                            uniqueFilenames.Add (uniqueFilename);
                        }
                        numResults++;
                    }

                    //Log.Debug ("startIndex=", startIndex, ", numResults=", numResults);
                    result.AddCompletedQuery (count: numResults);

                } while (numResults > 999);
            }, errorMessage: out errorMessage, catchAllExceptions: true, retryTimes: 3);

            return result;
        }

        private void CreateMissingWebAlbums (PictureShare share)
        {
            if (share.Albums.Count > 0) {
                // list local albums
                Log.Message ("List local albums:");
                Log.Indent++;
                foreach (Album localAlbum in share.Albums) {
                    if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {
                        string albumTitle = PhotoSyncUtilities.ToSyncedAlbumName (localAlbum);
                        Log.Message ("- ", Log.FillOrCut (text: albumTitle, length: 60, ending: "...]"), " files: ", string.Join (", ", localAlbum.Files.Count));
                    }
                }
                Log.Indent--;

                WebAlbum[] webAlbums = GetAlbums ();

                // create missing web albums
                Log.Message ("Create non-existant web albums:");
                Log.Indent++;
                int countNonExistantAlbums = 0;
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

                                countNonExistantAlbums++;
                            });
                            Log.Indent--;
                        }
                    }
                }
                if (countNonExistantAlbums == 0) {
                    Log.Message ("All local albums have web album counterparts.");
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

            object logLock = new object ();
            Parallel.ForEach<WebAlbum> (
                source: GetAlbums ().Where (a => PhotoSyncUtilities.IsSyncedAlbum (a)).OrderBy (a => a.Title),
                body: album => {

                    WebPhotoCollection result = GetPhotos (album: album);
                    lock (webAlbums) {
                        webAlbums [album] = result.WebFiles;
                    }

                    bool deleted = false;
                    // if the album doesn't exist locally...
                    if (!share.Albums.Any (a => album.Title == PhotoSyncUtilities.ToSyncedAlbumName (a))) {
                        // ... and doesn't contain any photos, delete it!
                        if (result.WebFiles.Length == 0) {
                            album.Delete (verbose: false);
                            result.Log ("Delete album.");
                            deleted = true;
                        }
                        // .. if it contains something, print it! it may be obsolete.
                        else {
                            result.Log ("Album does not exist locally.");
                        }
                    }

                    if (!deleted) {
                        lock (logLock) {
                            Log.Message ("- ", Log.FillOrCut (text: album.Title, length: 60, ending: "...]"), " files: ", string.Join (", ", result.NumResults));
                            Log.Indent += 2;
                            foreach (object[] message in result.Messages) {
                                Log.Message (message);
                            }
                            Log.Indent -= 2;
                        }
                    }
                }
            );
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
                        WebPhoto[] webFiles = webAlbums [webAlbum];

                        AlbumSyncStatus syncStatus = new AlbumSyncStatus (localAlbum: localAlbum, webAlbum: webAlbum, webFiles: webFiles);
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
                new[] {
                    "Album",
                    "Photos (local)",
                    "Videos (local)",
                    "All (local)",
                    "All (web)",
                    "Upload",
                    "Download"
                },
                s => s.WebAlbum.Title,
                s => s.LocalPhotos.Length,
                s => s.LocalVideos.Length,
                s => s.LocalFiles.Length,
                s => s.WebFiles.Length,
                s => s.FilesOnlyInLocalAlbum.Length,
                s => s.FilesOnlyInWebAlbum.Length
            ));
            Log.Indent--;
        }

        private void UploadDifferences (AlbumSyncStatus[] syncStatusList, Type[] selectedTypes)
        {
            Log.Message ("Upload: ");
            Log.Indent++;
            int countUploadedFiles = 0;
            foreach (AlbumSyncStatus syncStatus in syncStatusList) {
                if (syncStatus.FilesOnlyInLocalAlbum.Length > 0) {
                    Log.Message ("Album: ", syncStatus.WebAlbum.Title);
                    Log.Indent++;
                    fs.Runtime.ClearTempFiles ();
                    foreach (MediaFile localFile in syncStatus.FilesOnlyInLocalAlbum) {
                        // skip the non-selected file types
                        if (!selectedTypes.Any (validType => validType.IsAssignableFrom (localFile.Medium.GetType ()))) {
                            continue;
                        }

                        // get the mime type
                        string fullPath = localFile.FullPath;
                        string mimeType = libMediaFile.GetMimeTypeByExtension (fullPath: localFile.FullPath);
                        // skip if the mime type is unknown
                        if (mimeType == null) {
                            Log.Error ("Unknown mime type: ", localFile.FullPath);
                            continue;
                        }

                        // for jpeg and png pictures, resize them
                        if (mimeType == "image/jpeg" || mimeType == "image/png") {
                            Log.Message ("Resize File: [", mimeType, "] ", localFile.Name);
                            string tempPath = fs.Runtime.GetTempFilename (fullPath);
                            if (ImageResizeUtilities.ResizeImage (sourcePath: fullPath, destPath: tempPath, mimeType: mimeType, maxHeight: 2048, maxWidth: 2048)) {
                                fullPath = tempPath;
                            }
                        }

                        // announce the upload
                        Log.Message ("Upload File: [", mimeType, "] ", localFile.Name);

                        // skip videos over 100 MiB to avoid the error "Video file size exceeds 104857600"
                        if (localFile.Medium is Video && localFile.Size > 1024 * 1024 * 100) {
                            Log.Message ("Video file size is over 100 MiB.");
                            continue;
                        }

                        countUploadedFiles++;

                        // perform the upload
                        string errorMessage;
                        CatchErrors (todo: () => {
                            Uri postUri = new Uri (PicasaQuery.CreatePicasaUri (account.Id, syncStatus.WebAlbum.Id));

                            System.IO.FileInfo fileInfo = new System.IO.FileInfo (fullPath);
                            System.IO.FileStream fileStream = fileInfo.OpenRead ();

                            if (localFile.Medium is Picture) {
                                PicasaEntry entry = (PicasaEntry)service.Insert (postUri, fileStream, mimeType, localFile.Name);
                            } else if (localFile.Medium is Video) {
                                PhotoEntry videoEntry = new PhotoEntry ();
                                videoEntry.Title = new AtomTextConstruct (AtomTextConstructElementType.Title, localFile.Name);//I would change this to read the file type, This is just an example
                                videoEntry.Summary = new AtomTextConstruct (AtomTextConstructElementType.Summary, "");
                                MediaFileSource source = new MediaFileSource (fileStream, localFile.Name, mimeType);
                                videoEntry.MediaSource = source;
                                PicasaEntry entry = service.Insert (postUri, videoEntry);
                                Log.Debug ("Uploaded entry: ", entry.Id);
                            }

                            fileStream.Close ();
                        }, errorMessage: out errorMessage, catchAllExceptions: true, retryTimes: 0);

                        // if the photo limit is reached, skip to the next album
                        if (errorMessage != null && errorMessage.Contains ("Photo limit reached")) {
                            break;
                        }
                    }
                    fs.Runtime.ClearTempFiles ();
                    Log.Indent--;
                }
            }
            if (countUploadedFiles == 0) {
                Log.Message ("No files to upload.");
            }
            Log.Indent--;
        }

        public void UploadShare (PictureShare share, Type[] selectedTypes)
        {
            // create missing web albums
            CreateMissingWebAlbums (share: share);

            // list photos in web albums
            Dictionary<WebAlbum, WebPhoto[]> webAlbums = ListWebAlbums (share: share);

            // compare local and web albums...
            AlbumSyncStatus[] syncStatusList = CompareLocalAndWebAlbums (share: share, webAlbums: webAlbums);

            // print the differences between local and web albums
            PrintSyncStatus (syncStatusList: syncStatusList);

            foreach (Type selectedType in selectedTypes) {
                // upload missing photos
                UploadDifferences (syncStatusList: syncStatusList, selectedTypes: new [] { selectedType });
            }
        }
    }
}


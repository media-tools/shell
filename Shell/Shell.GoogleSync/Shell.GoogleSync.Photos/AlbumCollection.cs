using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Google.GData.Client;
using Google.GData.Photos;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.GoogleSync.Core;
using Shell.Media;
using Shell.Media.Content;
using Shell.Media.Files;
using Picasa = Google.Picasa;
using System.Net;
using System.Text;

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

        public void PrintAlbums (Filter albumFilter)
        {
            WebAlbum[] albums = GetAlbums ();

            Log.Message (albums.Filter (albumFilter).ToStringTable (
                a => LogColor.Reset,
                new[] { "Title", "Id", "NumPhotos" },
                a => a.Title,
                a => a.Id,
                a => a.NumPhotos
            ));
        }

        public WebPhotoCollection GetPhotos (WebAlbum album, bool deleteDuplicates, bool holdInternals)
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

                    Log.Debug ("PhotoQuery: album.Id=", album.Id, ", startIndex=", startIndex);

                    PhotoQuery query = new PhotoQuery (PicasaQuery.CreatePicasaUri (account.Id, album.Id));
                    query.NumberToRetrieve = 1000;
                    query.StartIndex = startIndex;
                    // http://stackoverflow.com/questions/9935814/where-is-the-full-size-image-in-gdata-photos-query
                    // add the extra parameter to request full size images
                    query.ExtraParameters = "imgmax=d";
                    Log.Debug ("PhotoQuery: ", query.Uri);
                    PicasaFeed feed = service.Query (query);

                    /*
                    using (var client = new WebClient ()) {
                        string url = query.Uri + "&access_token=" + account.AccessToken;
                        byte[] xmlBytes = client.DownloadData (url);
                        Log.Debug (Encoding.UTF8.GetString (xmlBytes));
                    }
*/


                    foreach (PicasaEntry entry in feed.Entries) {
                        /*Log.Debug ("entry: ", string.Join ("    ", (entry.Media.Content.Attributes.Keys.Cast<string> ().Select (k => k + "=" + (entry.Media.Content.Attributes [k] as string)))));
                        Log.Debug ("entry: ", string.Join ("    ", (entry.ToString ())));
                        using (var sw = new StringWriter ()) {
                            using (var xw = XmlWriter.Create (sw)) {
                                // Build Xml with xw.

                                feed.SaveToXml (xw);
                            }
                            Log.Debug ("xml:", sw.ToString ());
                        }*/

                        Picasa.Photo internalPhoto = new Picasa.Photo ();
                        internalPhoto.AtomEntry = entry;
                        
                        string uniqueFilename = internalPhoto.Title;

                        if (deleteDuplicates) {
                            if (uniqueFilenames.Contains (uniqueFilename)) {
                                result.Log ("Delete duplicate: " + uniqueFilename);
                                CatchErrors (() => entry.Delete ());
                            } else {
                                WebPhoto photo = new WebPhoto (albumCollection: this, album: album, internalPhoto: internalPhoto, holdInternals: holdInternals);
                                result.AddWebFile (photo);
                                uniqueFilenames.Add (uniqueFilename);
                            }
                        } else {
                            WebPhoto photo = new WebPhoto (albumCollection: this, album: album, internalPhoto: internalPhoto, holdInternals: holdInternals);
                            result.AddWebFile (photo);
                            uniqueFilenames.Add (uniqueFilename);
                        }

                        numResults++;
                    }

                    //Log.Debug ("startIndex=", startIndex, ", numResults=", numResults);
                    result.AddCompletedQuery (count: numResults);

                } while (numResults > 999 && startIndex + numResults < 10000);
            }, errorMessage: out errorMessage, catchAllExceptions: true, retryTimes: 3);

            /*if (!deleteDuplicates) {
                foreach (WebPhoto photo in result.WebFiles) {
                    photo.HasUniqueName = result.WebFiles.Count (f => f.Title == photo.Title) == 1;
                    if (!photo.HasUniqueName) {
                        Log.Message ("Not unique file: ", photo.Title, ", Timestamp: ", photo.Timestamp, ", Filename: ", photo.Filename);
                    }
                }
            }*/

            return result;
        }

        private void CreateMissingWebAlbums (MediaShare share)
        {
            if (share.Database.Albums.Length > 0) {
                // create missing web albums
                Log.Message ("Create non-existant web albums:");
                Log.Indent++;

                WebAlbum[] webAlbums = GetAlbums ();

                int countNonExistantAlbums = 0;
                foreach (Album localAlbum in share.Database.Albums) {
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

        private void RenameWebAlbums ()
        {
            Log.Message ("Rename web albums to normalized names:");
            Log.Indent++;

            WebAlbum[] webAlbums = GetAlbums ();
            Dictionary<string,string> NewAlbumTitles = new Dictionary<string, string> ();

            int count = 0;
            foreach (WebAlbum webAlbum in webAlbums) {
                if (!NamingUtilities.IsNormalizedAlbumName (webAlbum.Title)) {
                    NewAlbumTitles [webAlbum.Title] = NamingUtilities.NormalizeAlbumName (webAlbum.Title);
                }
            }

            CatchErrors (() => {
                AlbumQuery query = new AlbumQuery (PicasaQuery.CreatePicasaUri (account.Id));
                PicasaFeed feed = service.Query (query);

                foreach (PicasaEntry entry in feed.Entries) {
                    if (NewAlbumTitles.ContainsKey (entry.Title.Text)) {
                        string newTitle = NewAlbumTitles [entry.Title.Text];
                        Log.Message ("Rename: ", entry.Title.Text, " => ", newTitle);
                        entry.Title.Text = newTitle;
                        entry.Summary.Text = newTitle;
                        entry.Update ();
                        count++;
                    }
                }
            });

            if (count == 0) {
                Log.Message ("All web albums have normalized names.");
            }

            Log.Indent--;
        }

        public void PrintLocalAlbums (MediaShare share)
        {
            // print list of local albums
            Log.Message ("List local albums:");
            Log.Indent++;
            foreach (Album localAlbum in share.Database.Albums) {
                if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {
                    string albumTitle = PhotoSyncUtilities.ToSyncedAlbumName (localAlbum);
                    Log.Message ("- ", Log.FillOrCut (text: albumTitle, length: 60, ending: "...]"), " files: ", string.Join (", ", localAlbum.Files.Count));
                }
            }
            Log.Indent--;
        }

        private Dictionary<WebAlbum, WebPhoto[]> ListWebAlbums (MediaShare share, Filter albumFilter, bool onlySyncedAlbums, bool holdInternals)
        {
            // list photos in web albums
            Log.Message ("List photos in web albums: ");
            Log.Indent++;

            Dictionary<WebAlbum, WebPhoto[]> webAlbums = new Dictionary<WebAlbum, WebPhoto[]> ();
            WebAlbum[] source = GetAlbums ()
                .Where (a => PhotoSyncUtilities.IsSyncedAlbum (a) || !onlySyncedAlbums)
                .Filter (albumFilter)
                .OrderBy (a => a.Title).ToArray ();

            if (source.Length > 0) {
                Log.Debug ("count of selected web albums: ", source.Length);

                // open progress bar
                ProgressBar progress = Log.OpenProgressBar (identifier: "AlbumCollection:ListWebAlbums:" + share.RootDirectory, description: "List photos in web albums...");
                int i = 0;
                int max = source.Length;

                object logLock = new object ();
                CustomParallel.ForEach<WebAlbum> (
                    source: source,
                    body: album => {
                        Log.Debug ("- ", album.Title);

                        WebPhotoCollection result = GetPhotos (album: album, deleteDuplicates: onlySyncedAlbums, holdInternals: holdInternals);
                        lock (webAlbums) {
                            webAlbums [album] = result.WebFiles;
                        }

                        bool deleted = false;
                        // if the album doesn't exist locally...
                        if (!share.Database.Albums.Any (a => album.Title == PhotoSyncUtilities.ToSyncedAlbumName (a))) {
                            // .. and if it is no "special" album...
                            if (album.Title != PhotoSyncUtilities.SPECIAL_ALBUM_AUTO_BACKUP && !album.Title.StartsWith (PhotoSyncUtilities.SPECIAL_ALBUM_HANGOUT)) {
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
                        }

                        if (!deleted) {
                            //Log.Message ("- ", Log.FillOrCut (text: album.Title, length: 60, ending: "...]"), " files: ", string.Join (", ", result.NumResults));
                            if (result.Messages.Length > 0) {
                                lock (logLock) {
                                    Log.Message ("- ", album.Title, ":");
                                    Log.Indent += 2;
                                    foreach (object[] message in result.Messages) {
                                        Log.Message (message);
                                    }
                                    Log.Indent -= 2;
                                }
                            }
                        }

                        string progressDescription = "album: " + album.Title + ", " + (deleted ? "deleted." : "files: " + string.Join (", ", result.NumResults));

                        lock (logLock) {
                            progress.Print (current: i, min: 0, max: max, currentDescription: progressDescription, showETA: true, updateETA: false);
                            ++i;
                        }
                    }
                );

                progress.Finish ();
            } else {
                Log.Message ("No web albums are available or have been selected.");
            }

            Log.Indent--;

            GC.Collect ();
            GC.WaitForPendingFinalizers ();

            return webAlbums;
        }

        private AlbumSyncStatus[] CompareLocalAndWebAlbums (MediaShare share, Dictionary<WebAlbum, WebPhoto[]> webAlbums, Filter albumFilter)
        {
            // compare local and web albums...
            Log.Message ("Compare local and web albums:");
            Log.Indent++;

            List<AlbumSyncStatus> syncStatusList = new List<AlbumSyncStatus> ();
            foreach (Album localAlbum in share.Database.Albums) {
                if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {
                    string webAlbumName = PhotoSyncUtilities.ToSyncedAlbumName (localAlbum);

                    // check if the current album's name is accepted by the filter
                    if (!albumFilter.Matches (localAlbum)) {
                        // skip the current album if it is not
                        continue;
                    }

                    if (webAlbums.Keys.Any (wa => wa.Title == webAlbumName)) {
                        WebAlbum webAlbum = webAlbums.Keys.First (wa => wa.Title == webAlbumName);
                        WebPhoto[] webFiles = webAlbums [webAlbum];

                        AlbumSyncStatus syncStatus = new AlbumSyncStatus (localAlbum: localAlbum, webAlbum: webAlbum, webFiles: webFiles,
                                                         requireStrictFilenames: false, acceptDifferentVideoExtensions: false);
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
            if (syncStatusList.Length > 0) {
                Log.Message (syncStatusList.ToStringTable (
                    s => LogColor.Reset,
                    new[] {
                        "Album",
                        "Photos (local)",
                        "Videos (local)",
                        "All (local)",
                        "All (web)",
                        "Upload",
                        "Download",
                        "HQ ?"
                    },
                    s => s.WebAlbum.Title,
                    s => s.LocalPhotos.Length,
                    s => s.LocalVideos.Length,
                    s => s.LocalFiles.Length,
                    s => s.WebFiles.Length,
                    s => s.FilesOnlyInLocalAlbum.Length,
                    s => s.FilesOnlyInWebAlbum.Length,
                    s => s.LocalAlbum.IsHighQuality ? "HQ" : ""
                ));
            } else {
                
                Log.Message ("No albums selected.");
            }
            Log.Indent--;
        }



        private void DeleteExcessWebFiles (AlbumSyncStatus[] syncStatusList)
        {
            int countExcessFiles = syncStatusList.Select (ass => ass.FilesOnlyInWebAlbum.Length).Sum ();
            int deletedExcessFiles = 0;

            Log.Message ("Delete excess web files (#", countExcessFiles, "): ");
            Log.Indent++;
            int countUploadedFiles = 0;
            foreach (AlbumSyncStatus syncStatus in syncStatusList) {
                if (syncStatus.FilesOnlyInWebAlbum.Length > 0) {
                    Log.Message ("Album: ", syncStatus.WebAlbum.Title);
                    Log.Indent++;

                    HashSet<string> deletableFileIds = syncStatus.FilesOnlyInWebAlbum.Select (wf => wf.Id).ToHashSet ();

                    CatchErrors (() => {
                        WebPhotoCollection result = GetPhotos (album: syncStatus.WebAlbum, deleteDuplicates: true, holdInternals: true);
                        if (result.WebFiles.Length > 0) {

                            foreach (WebPhoto webFile in result.WebFiles.Where(wf => deletableFileIds.Contains(wf.Id))) {
                                // Log.Message ("Delete web file: ", webFile.FilenameForDownload, (webFile.Filename != webFile.FilenameForDownload ? " aka " + webFile.Filename : string.Empty));
                                deletedExcessFiles++;

                                Log.Message ("Delete web file #", deletedExcessFiles, " of #", countExcessFiles, ": ", webFile.Filename);
                                webFile.Delete ();
                            }

                        } else {
                            Log.Message ("No web files to delete.");
                        }
                    });

                    Log.Indent--;
                }
            }
            if (countUploadedFiles == 0) {
                Log.Message ("No web files to delete.");
            }
            Log.Indent--;
        }

        private void UploadDifferences (AlbumSyncStatus[] syncStatusList, Type[] selectedTypes)
        {
            Log.Message ("Upload local files: ");
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
                        string mimeType = localFile.Medium.MimeType;
                        // skip if the mime type is unknown
                        if (mimeType == "image/x-xcf") {
                            Log.Error ("Non-uploadable mime type: ", localFile.FullPath);
                            continue;
                        }

                        // for jpeg and png pictures, resize them
                        if ((mimeType == "image/jpeg" || mimeType == "image/png") && !syncStatus.LocalAlbum.IsHighQuality) {
                            Log.Message ("Resize File: [", mimeType, "] ", localFile.Filename);
                            string tempPath = fs.Runtime.GetTempFilename (fullPath);
                            if (ImageResizeUtilities.ResizeImage (sourcePath: fullPath, destPath: tempPath, mimeType: mimeType, maxHeight: 2048, maxWidth: 2048)) {
                                fullPath = tempPath;
                            }
                        }

                        // announce the upload
                        Log.Message ("Upload File: [", mimeType, "] ", localFile.Filename);

                        // skip videos over 100 MiB to avoid the error "Video file size exceeds 104857600"
                        if (localFile.Medium is Video && localFile.Size > 1024 * 1024 * 100) {
                            Log.Message ("Video file size is over 100 MiB.");
                            continue;
                        }

                        // refresh the account access token before uploading large files
                        if (localFile.Size > 1024 * 1024 * 50) {
                            RefreshAccount ();
                        }

                        countUploadedFiles++;

                        // perform the upload
                        string errorMessage;
                        CatchErrors (todo: () => {
                            Uri postUri = new Uri (PicasaQuery.CreatePicasaUri (account.Id, syncStatus.WebAlbum.Id));

                            System.IO.FileInfo fileInfo = new System.IO.FileInfo (fullPath);
                            System.IO.FileStream fileStream = fileInfo.OpenRead ();

                            if (localFile.Medium is Picture) {
                                PicasaEntry entry = (PicasaEntry)service.Insert (postUri, fileStream, mimeType, localFile.Filename);
                            } else if (localFile.Medium is Video) {
                                PhotoEntry videoEntry = new PhotoEntry ();
                                videoEntry.Title = new AtomTextConstruct (AtomTextConstructElementType.Title, localFile.Filename);//I would change this to read the file type, This is just an example
                                videoEntry.Summary = new AtomTextConstruct (AtomTextConstructElementType.Summary, "");
                                MediaFileSource source = new MediaFileSource (fileStream, localFile.Filename, mimeType);
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
                Log.Message ("No local files to upload.");
            }
            Log.Indent--;
        }

        public void UploadShare (MediaShare share, Type[] selectedTypes, Filter albumFilter, bool deleteExcess)
        {
            // rename web albums to normalized names
            RenameWebAlbums ();

            // create missing web albums
            CreateMissingWebAlbums (share: share);

            // list photos in web albums
            Dictionary<WebAlbum, WebPhoto[]> webAlbums = ListWebAlbums (share: share, albumFilter: albumFilter, onlySyncedAlbums: true, holdInternals: false);

            // compare local and web albums...
            AlbumSyncStatus[] syncStatusList = CompareLocalAndWebAlbums (share: share, webAlbums: webAlbums, albumFilter: albumFilter);

            // print the differences between local and web albums
            PrintSyncStatus (syncStatusList: syncStatusList);

            if (deleteExcess) {
                // delete excess files in web albums
                DeleteExcessWebFiles (syncStatusList: syncStatusList);
            } else {
                Log.Message ("Don't delete excess web files!");
            }

            foreach (Type selectedType in selectedTypes) {
                // upload missing photos
                UploadDifferences (syncStatusList: syncStatusList, selectedTypes: new [] { selectedType });
            }
        }

        private Dictionary<WebAlbum, WebPhoto[]> FilterLocallyUnindexedFiles (MediaShare[] shares, Dictionary<WebAlbum, WebPhoto[]> webAlbums, Filter deletableWebAlbums)
        {
            // compare local and web albums...
            Log.Message ("Filter locally unindexed files:");
            Log.Indent++;

            Dictionary<WebAlbum, WebPhoto[]> webAlbumsUnindexed = webAlbums.ToDictionary (entry => entry.Key, entry => entry.Value);

            foreach (MediaShare share in shares) {
                foreach (Album localAlbum in share.Database.Albums) {
                    if (PhotoSyncUtilities.IsIncludedInSync (localAlbum)) {

                        foreach (WebAlbum webAlbum in webAlbumsUnindexed.Keys.ToArray()) {
                            WebPhoto[] allWebFiles = webAlbumsUnindexed [webAlbum];

                            // only those files which are not in the local album are unindexed!
                            AlbumSyncStatus syncStatus = new AlbumSyncStatus (localAlbum: localAlbum, webAlbum: webAlbum, webFiles: allWebFiles,
                                                             requireStrictFilenames: true, acceptDifferentVideoExtensions: true);
                            WebPhoto[] unindexedWebFiles = syncStatus.FilesOnlyInWebAlbum;

                            webAlbumsUnindexed [webAlbum] = unindexedWebFiles;

                            if (deletableWebAlbums.Matches (webAlbum)) {
                                foreach (WebPhoto webFile in allWebFiles.Except(unindexedWebFiles)) {
                                    Log.Message ("- [", share.Name, "] [", localAlbum.AlbumPath, "] contains: ", webFile.FilenameForDownload, " aka ", webFile.Filename);
                                    CatchErrors (() => webFile.Delete ());
                                }
                            }
                        }
                    }
                }
            }

            Log.Message (webAlbums.Keys.ToStringTable (
                a => LogColor.Reset,
                new[] {
                    "Web Album",
                    "Files",
                    "Locally Indexed Files",
                    "Locally Unindexed Files",
                },
                a => a.Title,
                a => webAlbums [a].Length,
                a => webAlbums [a].Length - webAlbumsUnindexed [a].Length,
                a => webAlbumsUnindexed [a].Length
            ));

            Log.Indent--;

            return webAlbumsUnindexed;
        }

        public void DownloadFiles (string localDirectory, WebPhoto[] webFiles)
        {
            Downloader downloader = new Downloader ();

            try {
                Directory.CreateDirectory (localDirectory);
            } catch (IOException ex) {
                Log.Error (ex);
                return;
            }

            foreach (WebPhoto webFile in webFiles) {

                // if it's a picture!
                if (Picture.IsValidFile (fullPath: webFile.FilenameForDownload)) {
                    string localPath = Path.Combine (localDirectory, webFile.FilenameForDownload);

                    DownloadFile (localPath: localPath, webFile: webFile, downloader: downloader, kindOfFile: "Photo");
                }

                // if it's a video!
                else if (Video.IsValidFile (fullPath: webFile.FilenameForDownload) && webFile.MimeType.StartsWith ("video")) {
                    string localPath = Path.Combine (localDirectory, NamingUtilities.MakeRawFilename (webFile.FilenameForDownload));

                    DownloadFile (localPath: localPath, webFile: webFile, downloader: downloader, kindOfFile: "Video");
                }

                // if we only have a thumbnail of a video!
                else if (Video.IsValidFile (fullPath: webFile.FilenameForDownload) && webFile.MimeType == "image/gif") {
                    string localPath = Path.Combine (localDirectory, Path.GetFileNameWithoutExtension (webFile.FilenameForDownload) + "-thumbnail.gif");

                    DownloadFile (localPath: localPath, webFile: webFile, downloader: downloader, kindOfFile: "Video Thumbnail");
                }

                // we can't download any non-picture and non-video right now
                else {
                    Log.Message ("Skip File: [", webFile.MimeType, "] ", webFile.FilenameForDownload);
                }
            }
        }

        private void DownloadFile (string localPath, WebPhoto webFile, Downloader downloader, string kindOfFile)
        {
            // if the photo already exists
            if (File.Exists (localPath)) {
                Log.Message ("Skip " + kindOfFile + ": [", webFile.MimeType, "] ", webFile.FilenameForDownload, " (already exists)");
            }
            // download the photo
            else {
                // announce the download
                Log.Message ("Download " + kindOfFile + ": [", webFile.MimeType, "] ", webFile.FilenameForDownload,
                    (webFile.FilenameForDownload != webFile.Filename ? " aka " + webFile.Filename : ""));
                Log.Indent++;

                bool success = downloader.DownloadFile (localPath: localPath, url: webFile.DownloadUrl);
                if (success) {
                    success = downloader.SetTimestamp (localPath: localPath, timestamp: webFile.Timestamp);
                }
                if (!success) {
                    Log.Error ("Error.");
                }

                Log.Indent--;
            }
        }

        public void DownloadFiles (MediaShare share, WebAlbum album, WebPhoto[] webFiles)
        {
            Log.Message ("Album: ", album.Title);
            Log.Indent++;

            DownloadFiles (localDirectory: Path.Combine (share.RootDirectory, PhotoSyncUtilities.SPECIAL_ALBUM_AUTO_BACKUP), webFiles: webFiles);

            Log.Indent--;
        }

        public void DownloadAutoBackup (MediaShare share, Type[] selectedTypes, MediaShare[] otherShares)
        {
            Filter webAlbumAutoBackup = Filter.ExactFilter (PhotoSyncUtilities.SPECIAL_ALBUM_AUTO_BACKUP);
            Filter webAlbumHangouts = Filter.ContainFilter (PhotoSyncUtilities.SPECIAL_ALBUM_HANGOUT);
            Filter webAlbumDateTitle = Filter.RegexFilter (PhotoSyncUtilities.SPECIAL_ALBUM_DATE_TITLE_REGEX);
            Filter downloadableWebAlbums = Filter.Or (webAlbumAutoBackup, webAlbumHangouts, webAlbumDateTitle);
            Filter deletableWebAlbums = Filter.Or (webAlbumAutoBackup, webAlbumDateTitle);

            // list photos in web albums
            Dictionary<WebAlbum, WebPhoto[]> webAlbums = ListWebAlbums (share: share, albumFilter: downloadableWebAlbums, onlySyncedAlbums: false, holdInternals: true);

            // look for files which are already present in our share's any any other shares's local albums
            MediaShare[] allShares = new MediaShare[]{ share }.Concat (otherShares).ToHashSet ().ToArray ();
            // only delete stuff in the "Auto Backup" album, not in the "Hangouts:"-Albums !!
            Dictionary<WebAlbum, WebPhoto[]> webAlbumsUnindexed = FilterLocallyUnindexedFiles (shares: allShares, webAlbums: webAlbums, deletableWebAlbums: deletableWebAlbums);


            // compare local and web albums...
            Log.Message ("Download locally unindexed files:");
            Log.Indent++;

            // download all locally unindexed files!
            foreach (WebAlbum webAlbum in webAlbumsUnindexed.Keys) {
                for (int year = 1971; year < 2050; ++year) {
                    WebPhoto[] photosFromThatYear = webAlbumsUnindexed [webAlbum].Where (f => f.Timestamp.IsInYear (year)).ToArray ();
                    if (photosFromThatYear.Length > 0) {
                        string albumName = share.SpecialAlbumPrefix + PhotoSyncUtilities.SPECIAL_ALBUM_AUTO_BACKUP + " " + year;
                        Log.Message ("Album: [", albumName, "] (Year: ", year, ")");
                        Log.Indent++;
                        DownloadFiles (localDirectory: Path.Combine (share.RootDirectory, albumName), webFiles: photosFromThatYear);
                        Log.Indent--;
                    }
                }
            }

            Log.Indent--;
        }
    }
}


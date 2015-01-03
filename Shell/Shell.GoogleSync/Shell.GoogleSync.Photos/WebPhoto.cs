using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Google.GData.Photos;
using Google.Picasa;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.GoogleSync.Photos
{
    public class WebPhoto
    {
        public AlbumCollection AlbumCollection { get; private set; }

        public WebAlbum Album { get; private set; }

        //private Photo InternalPhoto { get; set; }

        // public string _Title { get; private set; }

        public string Id { get; private set; }

        public Size Dimensions { get; private set; }

        public ulong TimestampUnix { get; private set; }

        public DateTime Timestamp { get { return DateTimeExtensions.MillisecondsTimeStampToDateTime (TimestampUnix); } }

        public string Filename { get; private set; }

        public string FilenameForDownload { get; private set; }

        public string MimeType { get; private set; }

        public string DownloadUrl { get; private set; }

        private PicasaEntry PicasaEntry { get; set; }

        public WebPhoto (AlbumCollection albumCollection, WebAlbum album, Photo internalPhoto, bool holdInternals)
        {
            AlbumCollection = albumCollection;
            Album = album;
            Filename = internalPhoto.Title;
            Id = internalPhoto.Id;
            try {
                TimestampUnix = internalPhoto.Timestamp;
            } catch (OverflowException) {
                TimestampUnix = (ulong)DateTime.Now.ToMillisecondsTimestamp ();
                Log.Debug ("Fuck: ", Timestamp.ToString ());
            }
            Dimensions = new Size (internalPhoto.Width, internalPhoto.Height);
            MimeType = internalPhoto.PicasaEntry.Content.Type;
            DownloadUrl = internalPhoto.PicasaEntry.Content.AbsoluteUri;

            // the file name has to be platform independent
            Filename = Filename.Replace (":", "_").Trim ('_', '.', ' ', '~');
            // the file ending has to be in lower case
            Filename = Path.GetFileNameWithoutExtension (Filename) + Path.GetExtension (Filename).ToLower ();


            if (holdInternals) {
                PicasaEntry = internalPhoto.PicasaEntry;
            }

            string betterFilename = Filename;
            string username = albumCollection.GoogleAccount.ShortDisplayName;
            if (Filename.Length > 12 && Filename.StartsWith ("IMG-") && Filename.Contains ("-WA") && Regex.IsMatch (Filename, patternImgWa)) {
                if (Filename.Substring (4, 8) == Timestamp.ToString ("yyyyMMdd")) {
                    betterFilename = Regex.Replace (Filename, patternImgWa, "IMG_" + Timestamp.ToString ("yyyyMMdd_HHmmss") + "_WA");
                } else {
                    betterFilename = Timestamp.ToString ("yyyyMMdd_HHmmss_") + Filename;
                }
                Log.Debug ("Better Filename: ", Filename, " => ", betterFilename);
            } else if (Filename == "MOVIE.m4v") {
                betterFilename = "MOVIE_" + Timestamp.ToString ("yyyyMMdd_HHmmss") + ".m4v";
            } else if (!Regex.IsMatch (Filename, patternDate)) {
                betterFilename = Timestamp.ToString ("yyyyMMdd_HHmmss_") + Filename;
            }
            betterFilename = username + "_" + betterFilename;
            FilenameForDownload = betterFilename;

            //string oddName = Timestamp.ToString ("yyyyMMdd_") + Filename;
            //Log.DebugLog ("OddName3|", Filename, "|", oddName, "|", FilenameForDownload);

            //Log.Debug ("FilenameForDownload=", FilenameForDownload, " , Filename=", Filename, " , size=", internalPhoto.Size);
            //if (Title.ToLower ().Contains ("mp4")) {
            //    Log.Debug (internalPhoto.PhotoUri);
            //}

        }

        private static string patternImgWa = "IMG-((?:19|20)[0-9]{2})([0-9]{2})([0-9]{2})-WA";
        private static string patternDate = "((?:19|20)[0-9]{2})([0-1][0-9])([0-3][0-9])";

        public void Delete ()
        {
            if (PicasaEntry != null) {
                PicasaEntry.Delete ();
            } else {
                Log.Message ("Delete not possible! WebPhoto holds no internal object! FilenameForDownload=", FilenameForDownload);
            }
        }
    }
}


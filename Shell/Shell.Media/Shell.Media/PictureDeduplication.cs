using System;
using System.Collections.Generic;
using Shell.Media.Files;
using Shell.Media.Content;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Text.RegularExpressions;
using System.IO;

namespace Shell.Media
{
    public class PictureDeduplication
    {
        private static PictureLibrary lib = new PictureLibrary ();

        private static Regex regexPatternDate = new Regex ("((?:19|20)[0-9]{2})([0-1][0-9])([0-3][0-9])");

        public PictureDeduplication ()
        {
        }

        public void SetAuthor (MediaShare share, Filter albumFilter, string author, bool dryRun)
        {
            share.CleanIndex ();
            share.UpdateIndex ();
            share.Deserialize (verbose: true);

            Log.Message ("Set author: ", author);
            Log.Indent++;

            bool deletedSomething = false;

            foreach (Album album in share.Database.Albums.Filter(albumFilter).ToArray()) {
                foreach (MediaFile file in album.Files.ToArray()) {
                    string oldFilename = file.Filename;

                    // if the filename is in the preferred format
                    if (NamingUtilities.IsPreferredFileName (oldFilename)) {
                        Log.Message ("- [", album.AlbumPath, "] ", LogColor.DarkBlue, "[", oldFilename, "]", LogColor.Reset);
                    }
                    // otherwise add the author and the date!
                    else {
                        DateTime date = file.PreferredTimestamp; 
                        string preferredFilename = NamingUtilities.MakePreferredFileName (fileName: oldFilename, date: date, author: author);

                        Log.Message ("- [", album.AlbumPath, "] ", LogColor.DarkRed, "[", oldFilename, "]", LogColor.Reset,
                            " => ", LogColor.DarkGreen, "[", preferredFilename, "]", LogColor.Reset);

                        if (!dryRun) {
                            string oldPath = file.FullPath;
                            string newPath = Path.Combine (share.RootDirectory, album.AlbumPath, preferredFilename);

                            RenameFile (oldPath: oldPath, newPath: newPath);

                            file.IsDeleted = true;
                            deletedSomething = true;
                        }
                    }
                }
            }

            if (deletedSomething) {
                share.Serialize (verbose: true);
                share.CleanIndex ();
                share.UpdateIndex ();
            }

            Log.Indent--;
        }

        public void DeduplicateShare (MediaShare share, Filter albumFilter, bool dryRun)
        {
            share.CleanIndex ();
            share.UpdateIndex ();
            share.Deserialize (verbose: true);

            Log.Message ("Deduplicate share...");
            Log.Indent++;

            bool deletedSomething = false;

            Dictionary<HexString, MediaFileInAlbum[]> byPixelHash = GetPicturesByPixelHash (share: share, albumFilter: albumFilter);

            byPixelHash = byPixelHash.Keys.Where (key => byPixelHash [key].Length >= 2).ToDictionary (key => key, key => byPixelHash [key]);

            if (byPixelHash.Count > 0) {
                foreach (HexString pixelHash in byPixelHash.Keys.OrderBy(h => h.Hash)) {
                    Log.Message ("- [", pixelHash.PrintShort, "]");
                    Log.Indent++;

                    MediaFileInAlbum[] duplicateFiles = byPixelHash [pixelHash];
                    MediaFile bestFile;
                    string bestFilename;
                    FindBestFilename (candidates: duplicateFiles.Select (fileInAlbum => fileInAlbum.File), fileToKeep: out bestFile, bestFilename: out bestFilename);

                    if (bestFile != null) {
                        bool anyOtherFilenames = duplicateFiles.Any (df => df.File.Filename != bestFile.Filename);
                        bool anyBadFilename = duplicateFiles.Any (df => df.File.Filename != bestFilename);

                        // if there is a filename that's better than the others
                        if (anyOtherFilenames || anyBadFilename) {
                            foreach (MediaFileInAlbum dupFile in duplicateFiles) {
                                if (dupFile.File.Filename == bestFilename) {
                                    Log.Message ("- [", dupFile.Album.AlbumPath, "] ", LogColor.DarkGreen, "[", dupFile.File.Filename, "]", LogColor.Reset);
                                } else {
                                    Log.Message ("- [", dupFile.Album.AlbumPath, "] ", LogColor.DarkRed, "[", dupFile.File.Filename, "]", LogColor.Reset,
                                        " => ", LogColor.DarkGreen, "[", bestFilename, "]", LogColor.Reset);

                                    if (!dryRun) {
                                        string oldPath = dupFile.File.FullPath;
                                        string newPath = Path.Combine (share.RootDirectory, dupFile.Album.AlbumPath, bestFilename);

                                        RenameFile (oldPath: oldPath, newPath: newPath);

                                        dupFile.File.IsDeleted = true;
                                        deletedSomething = true;
                                    }
                                }
                            }
                        }
                        
                        // if all filename are equal
                        else {
                            foreach (MediaFileInAlbum dupFile in duplicateFiles) {
                                Log.Message ("- [", dupFile.Album.AlbumPath, "] ", LogColor.DarkBlue, "[", dupFile.File.Filename, "]", LogColor.Reset);
                            }
                        }
                    }
                    // if there is nothing to do
                    else {
                        foreach (MediaFileInAlbum dupFile in duplicateFiles) {
                            Log.Debug ("- [", dupFile.Album.AlbumPath, "] [", dupFile.File.Filename, "]");
                        }
                    }

                    Log.Indent--;
                }
            } else {
                Log.Message ("No duplicates.");
            }

            if (deletedSomething) {
                share.Serialize (verbose: true);
                share.CleanIndex ();
                share.UpdateIndex ();
            }

            Log.Indent--;
        }

        private bool RenameFile (string oldPath, string newPath)
        {
            if (oldPath != newPath && File.Exists (oldPath)) {
                Log.Indent++;
                try {
                    Log.MessageConsole (LogColor.DarkMagenta, "[Move] ", oldPath, LogColor.Reset);
                    Log.MessageConsole (LogColor.DarkMagenta, "    => ", newPath, LogColor.Reset);
                    Log.MessageLog ("[Move] ", oldPath, " => ", newPath);

                    if (File.Exists (newPath)) {
                        File.Delete (oldPath);
                    } else {
                        File.Copy (oldPath, newPath, true);
                        if (File.Exists (newPath)) {
                            File.Delete (oldPath);
                        }
                    }

                } catch (IOException ex) {
                    Log.Error (ex);
                    return File.Exists (newPath) && !File.Exists (oldPath);
                }
                Log.Indent--;

                return File.Exists (newPath) && !File.Exists (oldPath);
            }
            return false;
        }

        private Dictionary<HexString, MediaFileInAlbum[]> GetPicturesByPixelHash (MediaShare share, Filter albumFilter)
        {
            Dictionary<HexString, MediaFileInAlbum[]> byPixelHash = new Dictionary<HexString, MediaFileInAlbum[]> ();

            foreach (Album album in share.Database.Albums.Filter(albumFilter).ToArray()) {
                foreach (MediaFile file in album.Files.ToArray()) {
                    if (file.Medium is Picture) {
                        HexString? pixelhash = (file.Medium as Picture).PixelHash;
                        if (pixelhash.HasValue && !string.IsNullOrEmpty (pixelhash.Value.Hash)) {
                            MediaFileInAlbum fileInAlbum = new MediaFileInAlbum {
                                File = file,
                                Album = album
                            };

                            if (byPixelHash.ContainsKey (pixelhash.Value))
                                byPixelHash [pixelhash.Value] = byPixelHash [pixelhash.Value].Concat (new [] { fileInAlbum }).ToArray ();
                            else
                                byPixelHash [pixelhash.Value] = new [] { fileInAlbum };
                        }
                    }
                }
            }

            return byPixelHash;
        }

        public void DeduplicateAlbums (MediaShare share, Filter albumFilter, bool dryRun)
        {
            share.CleanIndex ();
            share.Deserialize (verbose: true);

            Log.Message ("Deduplicate albums...");
            Log.Indent++;

            foreach (Album album in share.Database.Albums.Filter(albumFilter).ToArray()) {
                bool deletedSomething = false;

                Dictionary<DateTime, MediaFile[]> byTimestamp = SortByExifTimestamp (album.Files.ToArray ());
                if (byTimestamp.Count > 0) {
                    Log.Message ("Album: [", album.AlbumPath, "]");
                    Log.Indent++;

                    foreach (DateTime timestamp in byTimestamp.Keys) {
                        Log.Message ("Timestamp: [", timestamp.ToString ("yyyy:MM:dd HH:mm:ss"), "]");
                        Log.Indent++;

                        Dictionary<HexString, MediaFile[]> byPixelHash = new Dictionary<HexString, MediaFile[]> ();
                        foreach (MediaFile file in byTimestamp [timestamp]) {
                            // Log.Debug ("- [", file.Size, "] ", file.Filename);
                            HexString? pixelhash = (file.Medium as Picture).PixelHash;
                            if (pixelhash.HasValue) {
                                if (byPixelHash.ContainsKey (pixelhash.Value))
                                    byPixelHash [pixelhash.Value] = byPixelHash [pixelhash.Value].Concat (new MediaFile[] { file }).ToArray ();
                                else
                                    byPixelHash [pixelhash.Value] = new MediaFile[] { file };
                            }
                        }

                        byPixelHash = byPixelHash.Keys.Where (key => byPixelHash [key].Length >= 2).ToDictionary (key => key, key => byPixelHash [key]);

                        foreach (HexString pixelHash in byPixelHash.Keys) {
                            Log.Message ("Pixel Hash: [", pixelHash.PrintShort, "]");
                            Log.Indent++;

                            MediaFile[] sameFiles = byPixelHash [pixelHash];

                            MediaFile fileToKeep;
                            string __bestFilename;
                            FindBestFilename (candidates: sameFiles, fileToKeep: out fileToKeep, bestFilename: out __bestFilename);

                            foreach (MediaFile file in sameFiles) {
                                Log.Message ("- [", file.Size, "] ", file.Filename, file == fileToKeep ? " [keep!]" : "");
                            }

                            if (!dryRun) {
                                if (fileToKeep != null) {
                                    foreach (MediaFile file in sameFiles) {
                                        if (file != fileToKeep) {
                                            try {
                                                Log.Debug ("Delete: ", file.Filename);
                                                File.Delete (file.FullPath);
                                                file.IsDeleted = true;
                                                deletedSomething = true;
                                            } catch (Exception ex) {
                                                Log.Error (ex);
                                            }
                                        }
                                    }
                                }
                            }

                            Log.Indent--;
                        }

                        Log.Indent--;
                    }

                    Log.Indent--;
                }

                if (deletedSomething) {
                    share.Serialize (verbose: true);
                }
            }

            Log.Indent--;
        }

        private Dictionary<DateTime, MediaFile[]> SortByExifTimestamp (MediaFile[] files)
        {
            Dictionary<DateTime, List<MediaFile>> byTimestamp = new Dictionary<DateTime, List<MediaFile>> ();
            foreach (MediaFile file in files) {
                if (file.Medium is Picture && file.Extension != ".xcf") {
                    Picture pic = file.Medium as Picture;
                    DateTime? _timestamp = pic.ExifTimestampCreated;

                    if (_timestamp.HasValue) {
                        DateTime timestamp = _timestamp.Value;

                        if (!byTimestamp.ContainsKey (timestamp)) {
                            byTimestamp [timestamp] = new List<MediaFile> ();
                        }
                        List<MediaFile> list = byTimestamp [timestamp];
                        list.Add (file);
                        byTimestamp [timestamp] = list;
                    }
                }
            }
            return byTimestamp.Keys
                .Where (key => byTimestamp [key].Count >= 2)
                .Where (key => !(key.Minute == 0 && key.Second == 0))
                .ToDictionary (key => key, key => byTimestamp [key].ToArray ());
        }

        private struct FileWithDate
        {
            public MediaFile File;
            public DateTime? Date;

            public string Filename { get { return File.Filename; } }
        }

        public static bool FindBestFilename (IEnumerable<MediaFile> candidates, out MediaFile fileToKeep, out string bestFilename)
        {
            List<FileWithDate> _candidates = new List<FileWithDate> ();
            foreach (MediaFile file in candidates) {
                DateTime date;
                if (NamingUtilities.GetFileNameDate (file.Filename, out date)) {
                    _candidates.Add (new FileWithDate { File = file, Date = date });
                } else {
                    _candidates.Add (new FileWithDate { File = file, Date = null });
                }
            }
            
            DateTime? earliestDate = null;
            DateTime[] dates = _candidates.Where (tuple => tuple.Date.HasValue).Select (tuple => tuple.Date.Value).OrderBy (dt => dt).ToArray ();
            DateTime[] datesWithTime = dates.Where (dt => dt.HasTimeComponent ()).OrderBy (dt => dt).ToArray ();
            DateTime[] datesWithoutTime = dates.Where (dt => !dt.HasTimeComponent ()).OrderBy (dt => dt).ToArray ();
            if (datesWithTime.Any () && datesWithoutTime.Any ()) {
                DateTime earliestDateWithTime = datesWithTime.FirstOrDefault ();
                DateTime earliestDateWithoutTime = datesWithoutTime.FirstOrDefault ();
                if (earliestDateWithoutTime.Year < earliestDateWithTime.Year) {
                    earliestDate = earliestDateWithoutTime;
                } else {
                    earliestDate = earliestDateWithTime;
                }
            } else if (datesWithTime.Any ()) {
                earliestDate = datesWithTime.FirstOrDefault ();
            } else if (datesWithoutTime.Any ()) {
                earliestDate = datesWithoutTime.FirstOrDefault ();

            } else {
                Log.Debug ("earliestDate: none");
            }
            Log.Debug ("earliestDate: ", earliestDate.HasValue ? earliestDate.Value.StdFormat () : "none",
                " (all dates: ", string.Join (", ", dates.Select (dt => dt.StdFormat ())), ")",
                " (dates with time: ", string.Join (", ", datesWithTime.Select (dt => dt.StdFormat ())), ")",
                " (dates without time: ", string.Join (", ", datesWithoutTime.Select (dt => dt.StdFormat ())), ")"
            );
            return FindBestFilename (candidates: _candidates, earliestDate: earliestDate, fileToKeep: out fileToKeep, bestFilename: out bestFilename);
        }

        private static bool FindBestFilename (IEnumerable<FileWithDate> candidates, DateTime? earliestDate, out MediaFile fileToKeep, out string bestFilename)
        {
            IEnumerable<FileWithDate> preferredFiles = candidates
                .Where (file => NamingUtilities.IsPreferredFileName (file.Filename))
                .Where (file => !file.Filename.Contains ("000000"))
                .Where (file => !file.Filename.Contains ("llerlei"))
                .OrderByDescending (file => file.Filename.CountLetters ())
                .ThenByDescending (file => file.Filename.CountDigits ());

            if (!preferredFiles.Any ()) {
                preferredFiles = candidates
                    .Where (file => file.Filename.CountLetters () >= 10)
                    .Where (file => !file.Filename.Contains ("000000"))
                    .OrderByDescending (file => file.Filename.CountLetters ())
                    .ThenByDescending (file => file.Filename.CountDigits ());
            }

            if (!preferredFiles.Any ()) {
                preferredFiles = candidates
                    .Where (file => !file.Filename.Contains ("000000"))
                    .OrderByDescending (file => file.Filename.CountLetters ())
                    .ThenByDescending (file => file.Filename.CountDigits ());
            }

            if (!preferredFiles.Any ()) {
                preferredFiles = candidates.OrderBy (tuple => tuple.Date);
            }

            if (preferredFiles.Any ()) {
                fileToKeep = preferredFiles.First ().File;
                bestFilename = cleanFilename (filename: fileToKeep.Filename, earliestDate: earliestDate);
                Log.Debug ("FindBestFilename: bestFilename=", bestFilename, ", earliestDate=", earliestDate.HasValue ? earliestDate.Value.ToString () : "null");
                return true;
            } else {
                fileToKeep = null;
                bestFilename = null;
                Log.Debug ("FindBestFilename: bestFilename=", null, ", earliestDate=", earliestDate.HasValue ? earliestDate.Value.ToString () : "null");
                return false;
            }
        }

        private static string cleanFilename (string filename, DateTime? earliestDate)
        {
            filename = regexRemoveMeaninglessBraces.Replace (filename, "");

            if (!NamingUtilities.IsPreferredFileName (filename)) {
                if (earliestDate.HasValue) {
                    
                    string formattedEarliestDate = earliestDate.Value.ToString (format: "yyyyMMdd_HHmmss");

                    DateTime date;
                    if (NamingUtilities.GetFileNameDate (filename, out date)) {
                        if (date != earliestDate.Value) {
                            filename = formattedEarliestDate + "_" + filename;
                        }
                    } else {
                        filename = formattedEarliestDate + "_" + filename;
                    }
                }
            }
            return filename;
        }

        private static Regex regexRemoveMeaninglessBraces = new Regex (@"\s*[(][^a-zA-Z]*[)]\s*");
    }
}

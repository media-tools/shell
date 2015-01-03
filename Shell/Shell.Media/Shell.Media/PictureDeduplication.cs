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

        private static Regex regexRemoveNonLetters = new Regex ("[^a-zA-Z]");
        private static Regex regexPatternDate = new Regex ("((?:19|20)[0-9]{2})([0-1][0-9])([0-3][0-9])");

        public PictureDeduplication ()
        {
        }

        public void Deduplicate (MediaShare share, Filter albumFilter)
        {
            share.CleanIndex ();
            share.Deserialize (verbose: true);

            Log.Message ("Deduplicate...");
            Log.Indent++;

            foreach (Album album in share.Albums.Filter(albumFilter).ToArray()) {
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
                            Log.Message ("Pixel Hash: [", pixelHash.Hash.Substring (0, 8), "]");
                            Log.Indent++;

                            MediaFile[] sameFiles = byPixelHash [pixelHash];

                            IEnumerable<MediaFile> preferredFiles = sameFiles
                                .Where (file => NamingUtilities.IsPreferredFileName (file.Filename))
                                .OrderByDescending (file => regexRemoveNonLetters.Replace (file.Filename, "").Length);

                            MediaFile fileToKeep = preferredFiles.FirstOrDefault ();

                            if (fileToKeep == null && sameFiles.Any (file => file.Filename.Contains ("("))) {
                                preferredFiles = sameFiles
                                    .Where (file => !file.Filename.Contains ("("));
                                fileToKeep = preferredFiles.FirstOrDefault ();
                            }
                            if (fileToKeep == null) {
                                preferredFiles = sameFiles
                                    .Where (file => regexPatternDate.IsMatch (file.Filename))
                                    .OrderByDescending (file => regexRemoveNonLetters.Replace (file.Filename, "").Length);
                                fileToKeep = preferredFiles.FirstOrDefault ();
                            }
                            if (fileToKeep == null) {
                                preferredFiles = sameFiles
                                    .OrderByDescending (file => regexRemoveNonLetters.Replace (file.Filename, "").Length);
                                fileToKeep = preferredFiles.FirstOrDefault ();
                            }

                            foreach (MediaFile file in sameFiles) {
                                Log.Message ("- [", file.Size, "] ", file.Filename, file == fileToKeep ? " [keep!]" : "");
                            }

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

    }
}


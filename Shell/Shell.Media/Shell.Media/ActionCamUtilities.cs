using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Options;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;

namespace Shell.Media
{
    public class ActionCamUtilities
    {
        public void VideoMerge (string[] directories, bool dryRun)
        {
            Log.Message ("Action Cam Utilities: Video Merge");
            Log.Indent++;

            foreach (string dir in directories) {
                VideoMerge (directory: dir, dryRun: dryRun);
            }

            Log.Indent--;
        }

        public void VideoMerge (string directory, bool dryRun)
        {
            Log.Message ("[", directory, "]");
            Log.Indent++;

            try {
                string[] files = Directory.GetFiles (directory);
                string[] videos = files.Select (path => Path.GetFileName (path)).Where (name => patternSjcamVideo.IsMatch (name)).ToArray ();

                string[][] consecutiveVideos = FindConsecutiveVideos (videos);

                foreach (string[] consecution in consecutiveVideos) {
                    DateTime date;
                    if (NamingUtilities.GetFileNameDate (fileName: consecution.First (), date: out date)) {
                        string formattedDate = date.ToString (format: dateFormatSjcamVideo);

                        Log.Message ("[", formattedDate, "]");
                        Log.Indent++;

                        foreach (string fileName in consecution) {
                            Log.Message (fileName);
                        }

                        Log.Indent--;
                    }
                }

            } catch (IOException ex) {
                Log.Error (ex);
            }

            Log.Indent--;
        }

        private string[][] FindConsecutiveVideos (string[] files)
        {
            List<List<string>> consecutiveVideos = new List<List<string>> ();

            List<string> consecution = null;
            foreach (string currentfileName in files) {
                // if this is the first file
                if (consecution == null) {
                    consecution = new List<string> ();
                    consecution.Add (currentfileName);
                } else {
                    string previousFileName = consecution.Last ();

                    // if the current file is a consecutive file of the previous one
                    if (IsConsecutiveFile (fileA: previousFileName, fileB: currentfileName)) {
                        consecution.Add (currentfileName);
                    }
                    // otherwise...
                    else {
                        consecutiveVideos.Add (consecution);
                        consecution = new List<string> ();
                        consecution.Add (currentfileName);
                    }
                }
            }
            // for the last file
            if (consecution != null) {
                consecutiveVideos.Add (consecution);
            }

            return consecutiveVideos.Select (list => list.ToArray ()).ToArray ();
        }

        private bool IsConsecutiveFile (string fileA, string fileB)
        {
            DateTime dateOfA;
            DateTime dateOfB;

            if (NamingUtilities.GetFileNameDate (fileName: fileA, date: out dateOfA) && NamingUtilities.GetFileNameDate (fileName: fileB, date: out dateOfB)) {
                TimeSpan span = dateOfB - dateOfA;
                int numOfA = GetConsecutiveNumber (fileA);
                int numOfB = GetConsecutiveNumber (fileB);

                bool result = numOfB == numOfA + 1 && Math.Abs (span.TotalSeconds) <= 11 * 60;

                Log.Debug ("IsConsecutiveFile: fileA=", Path.GetFileName (fileA), ", fileB=", Path.GetFileName (fileB),
                    ", numOfA=", numOfA, ", numOfB=", numOfB,
                    ", dateOfA=", dateOfA, ", dateOfB", dateOfB,
                    ", result=", result);

                return result;
            } else {
                Log.Error ("BUG!");
                Log.Error ("Unable to determine the DateTime of one of the following files: '", fileA, "' or '", fileB, "'");
                return false;
            }
        }

        private static string dateFormatSjcamVideo = "yyyy_MMdd_HHmmss";
        private static Regex patternSjcamVideo = new Regex ("((?:19|20)[0-9]{2})_([0-9]{2})([0-9]{2})_([0-9]{6})_([0-9]{3})");

        private int GetConsecutiveNumber (string fileName)
        {
            try {
                Match match = patternSjcamVideo.Match (fileName);
                if (match.Success) {
                    string num = match.Groups [5].Value;
                    return int.Parse (num);
                } else {
                    Log.Error ("Unable to determine the consecutive number of the following file: '", fileName, "'");
                    return -1;
                }
            } catch (Exception ex) {
                Log.Error ("BUG!");
                Log.Error ("Unable to determine the consecutive number of the following file: '", fileName, "'");
                Log.Error (ex);
                return -1;
            }
        }
    }

}


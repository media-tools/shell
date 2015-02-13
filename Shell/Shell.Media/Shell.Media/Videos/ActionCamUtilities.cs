using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Media.Content;
using Shell.Media.Files;

namespace Shell.Media.Videos
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
                ActionCamVideo[] videos = files.Where (path => ActionCamVideo.IsValidVideoFile (path)).Select (path => new ActionCamVideo (path)).ToArray ();

                ActionCamVideo[][] consecutiveVideos = FindConsecutiveVideos (videos);

                foreach (ActionCamVideo[] consecution in consecutiveVideos) {

                    Log.Message ("[", consecution.First ().FormattedDate, "]");
                    Log.Indent++;

                    Log.Message ("Original video files:");
                    Log.Indent++;
                    foreach (ActionCamVideo video in consecution) {
                        Log.Message (video.FileName);
                    }
                    Log.Indent--;

                    ConvertContainerFormat (consecution: consecution);

                    if (consecution.Length >= 2) {
                        MergeConsecution (consecution: consecution);
                    }

                    Log.Indent--;
                }

            } catch (IOException ex) {
                Log.Error (ex);
            }

            Log.Indent--;
        }

        private void ConvertContainerFormat (ActionCamVideo[] consecution)
        {
            try {
                Log.Try ();

                if (consecution.Any (v => !v.FileName.EndsWith (".mkv"))) {
                    Log.Message ("Convert container format of files:");
                    Log.Indent++;

                    foreach (ActionCamVideo video in consecution) {
                        Log.Message ("[", video.FileName, "]");
                        Log.Indent++;

                        string newPath;
                        if (VideoLibrary.Instance.EncodeMatroska (fullPath: video.FullPath, outputPath: out newPath, encoding: VideoEncoding.COPY)) {
                            string newPathRaw = NamingUtilities.MakeRawFilename (newPath);
                            File.Move (newPath, newPathRaw);
                            video.FullPath = newPathRaw;
                        } else {
                            Log.Error ("Error while converting container format to matroska!");
                            return;
                        }

                        Log.Indent--;
                    }

                    Log.Indent--;
                }
            } catch (IOException ex) {
                Log.Error (ex);
            } finally {
                Log.Finally ();
            }
        }

        private void MergeConsecution (ActionCamVideo[] consecution)
        {
            ActionCamVideo first = consecution.First ();
            ActionCamVideo last = consecution.Last ();
            string targetFileName = NamingUtilities.MakeRawFilename (first.FormattedDate + "_" + first.FormattedConsecutiveNumber + "-" + last.FormattedConsecutiveNumber + ".mkv");
            string targetFullPath = Path.Combine (Path.GetDirectoryName (first.FullPath), targetFileName);

            Log.Message ("Merge into: ", targetFileName);
            Log.Indent++;

            if (VideoLibrary.Instance.MergeMatroska (outputPath: targetFullPath, inputPaths: consecution.Select (video => video.FullPath).ToArray ())) {
                Log.Message ("Success.");
            } else {
                Log.Message ("Failure.");
            }

            Log.Indent--;
        }

        private ActionCamVideo[][] FindConsecutiveVideos (ActionCamVideo[] files)
        {
            List<List<ActionCamVideo>> consecutiveVideos = new List<List<ActionCamVideo>> ();

            List<ActionCamVideo> consecution = null;
            foreach (ActionCamVideo currentVideo in files) {
                // if this is the first file
                if (consecution == null) {
                    consecution = new List<ActionCamVideo> ();
                    consecution.Add (currentVideo);
                } else {
                    ActionCamVideo previousVideo = consecution.Last ();

                    // if the current file is a consecutive file of the previous one
                    if (IsConsecutiveFile (fileA: previousVideo, fileB: currentVideo)) {
                        consecution.Add (currentVideo);
                    }
                    // otherwise...
                    else {
                        consecutiveVideos.Add (consecution);
                        consecution = new List<ActionCamVideo> ();
                        consecution.Add (currentVideo);
                    }
                }
            }
            // for the last file
            if (consecution != null) {
                consecutiveVideos.Add (consecution);
            }

            return consecutiveVideos.Select (list => list.ToArray ()).ToArray ();
        }

        private bool IsConsecutiveFile (ActionCamVideo fileA, ActionCamVideo fileB)
        {
            TimeSpan span = fileB.Date - fileA.Date;

            bool result = fileB.ConsecutiveNumber == fileA.ConsecutiveNumber + 1 && Math.Abs (span.TotalSeconds) <= 11 * 60;

            Log.Debug ("IsConsecutiveFile: fileA=", fileA.FileName, ", fileB=", fileB.FileName,
                ", numOfA=", fileA.ConsecutiveNumber, ", numOfB=", fileB.ConsecutiveNumber,
                ", dateOfA=", fileA.FormattedDate, ", dateOfB=", fileB.FormattedDate,
                ", result=", result);

            return result;
        }
    }
}


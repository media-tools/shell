using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Options;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Media.Content;
using Shell.Media.Files;
using Shell.Namespaces;

namespace Shell.Media.Videos
{
    public class VideoUtilities
    {
        public void VideoEncodeDirectory (string[] directories, VideoEncoding encoding, int crf, bool dryRun, int scaleX, int scaleY)
        {
            Log.Message ("Video Utilities: Video Encode");
            Log.Indent++;
            Log.Message ("Encoding: ", encoding);
            Log.Message ("CRF: ", crf);
            Log.Message ("Scale: ", scaleX, ":", scaleY);

            foreach (string dir in directories) {
                VideoEncodeDirectory (directory: dir, dryRun: dryRun, encoding: encoding, crf: crf, scaleX: scaleX, scaleY: scaleY);
            }

            Log.Indent--;
        }

        public void VideoEncodeDirectory (string directory, VideoEncoding encoding, int crf, bool dryRun, int scaleX, int scaleY)
        {
            Log.Message ("[", directory, "]");
            Log.Indent++;

            try {
                string[] files = Directory.GetFiles (directory);

                string[] rawVideoFiles = files.Where (NamingUtilities.IsRawFilename).ToArray ();

                foreach (string rawVideoFile in rawVideoFiles) {
                    VideoEncodeFile (rawVideoFile: rawVideoFile, encoding: encoding, crf: crf, dryRun: dryRun, scaleX: scaleX, scaleY: scaleY);
                }

            } catch (IOException ex) {
                Log.Error (ex);
            }

            Log.Indent--;
        }

        public void VideoEncodeFile (string rawVideoFile, VideoEncoding encoding, int crf, bool dryRun, int scaleX, int scaleY)
        {
            string targetPath = NamingUtilities.UnmakeRawFilename (fileName: rawVideoFile);
            if (!targetPath.EndsWith (".mkv")) {
                targetPath = Path.Combine (Path.GetDirectoryName (targetPath), Path.GetFileNameWithoutExtension (targetPath) + ".mkv");
            }
            if (scaleX != -1 || scaleY != -1) {
                int approxScaleY = (int)(Math.Round ((double)scaleY / 20.0) * 20.0);
                int approxScaleX = (int)(Math.Round ((double)scaleX / 20.0) * 20.0);
                string scaleString = scaleY != -1 ? approxScaleY + "p" : approxScaleX + "wp";
                string filename = Path.GetFileNameWithoutExtension (targetPath);
                string[] resolutionSubstrings = new [] { "1080p", "720p" };
                foreach (string resolutionSubstring in resolutionSubstrings) {
                    if (filename.Contains (resolutionSubstring)) {
                        filename = filename.Replace (resolutionSubstring, scaleString + ".from." + resolutionSubstring);
                        break;
                    }
                }
                if (!filename.Contains (scaleString)) {
                    filename += "." + scaleString;
                }
                targetPath = Path.Combine (Path.GetDirectoryName (targetPath), filename + ".mkv");
            }

            Log.Message ("[", Path.GetFileName (rawVideoFile), " => ", Path.GetFileName (targetPath), "]");
            Log.Indent++;

            Log.Debug ("source: ", rawVideoFile);
            Log.Debug ("destination: ", targetPath);

            VideoLibrary.Instance.EncodeMatroska (sourceFullPath: rawVideoFile, destinationFullPath: targetPath, encoding: encoding, crf: crf, scaleX: scaleX, scaleY: scaleY);

            Log.Indent--;
        }
    }

}


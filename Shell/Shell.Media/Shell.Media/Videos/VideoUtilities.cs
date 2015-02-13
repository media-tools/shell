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
        public void VideoEncodeDirectory (string[] directories, VideoEncoding encoding, int crf, bool dryRun)
        {
            Log.Message ("Video Utilities: Video Encode");
            Log.Indent++;
            Log.Message ("Encoding: ", encoding);
            Log.Message ("CRF: ", crf);

            foreach (string dir in directories) {
                VideoEncodeDirectory (directory: dir, dryRun: dryRun, encoding: encoding, crf: crf);
            }

            Log.Indent--;
        }

        public void VideoEncodeDirectory (string directory, VideoEncoding encoding, int crf, bool dryRun)
        {
            Log.Message ("[", directory, "]");
            Log.Indent++;

            try {
                string[] files = Directory.GetFiles (directory);

                string[] rawVideoFiles = files.Where (path => NamingUtilities.IsRawFilename (path)).ToArray ();

                foreach (string rawVideoFile in rawVideoFiles) {
                    VideoEncodeFile (rawVideoFile: rawVideoFile, encoding: encoding, crf: crf, dryRun: dryRun);
                }

            } catch (IOException ex) {
                Log.Error (ex);
            }

            Log.Indent--;
        }

        public void VideoEncodeFile (string rawVideoFile, VideoEncoding encoding, int crf, bool dryRun)
        {
            string targetPath = NamingUtilities.UnmakeRawFilename (fileName: rawVideoFile);
            if (!targetPath.EndsWith (".mkv"))
                targetPath = Path.Combine (Path.GetDirectoryName (targetPath), Path.GetFileNameWithoutExtension (targetPath) + ".mkv");

            Log.Message ("[", Path.GetFileName (rawVideoFile), " => ", Path.GetFileName (targetPath), "]");
            Log.Indent++;

            Log.Debug ("source: ", rawVideoFile);
            Log.Debug ("destination: ", targetPath);

            VideoLibrary.Instance.EncodeMatroska (sourceFullPath: rawVideoFile, destinationFullPath: targetPath, encoding: encoding, crf: crf);

            Log.Indent--;
        }
    }

}


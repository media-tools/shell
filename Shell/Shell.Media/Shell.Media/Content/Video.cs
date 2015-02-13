using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Util;
using Shell.Media.Database;
using Shell.Media.Files;
using Shell.Media.Videos;

namespace Shell.Media.Content
{
    public class Video : MediaFile
    {
        public static HashSet<string> FILE_ENDINGS = new [] {
            ".mkv",
            ".mp4",
            ".avi",
            ".flv",
            ".mov",
            ".mpg",
            ".3gp",
            ".m4v",
            ".divx",
            ".webm",
            ".wmv"
        }.ToHashSet ();

        public static Dictionary<string[],string[]> MIME_TYPES = new Dictionary<string[],string[]> () {
            { new [] { "video/mpeg4", "video/mp4" }, new [] { ".mp4" } },
            { new [] { "video/x-flv" }, new [] { ".flv" } },
            { new [] { "video/x-msvideo" }, new [] { ".avi" } },
            { new [] { "video/x-matroska" }, new [] { ".mkv" } },
            { new [] { "video/webm" }, new [] { ".webm" } },
            { new [] { "video/mpeg" }, new [] { ".mpg" } },
            { new [] { "video/ogg" }, new [] { ".ogv" } },
            { new [] { "video/x-ms-wmv" }, new [] { ".wmv" } },
            { new [] { "video/3gpp" }, new [] { ".3gp" } }
        };

        public override DateTime? PreferredTimestampInternal { get { return null; } }

        public Video (string fullPath, MediaDatabase database)
            : base (fullPath, database)
        {
        }

        public Video ()
        {
        }

        public static new bool IsValidFile (string fullPath)
        {
            return MediaShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void Index ()
        {
            if (string.IsNullOrWhiteSpace (MimeType)) {
                MimeType = libMediaFile.GetMimeTypeByExtension (fullPath: FullPath);
            }
        }

        public override bool IsCompletelyIndexed {
            get {
                return !string.IsNullOrWhiteSpace (MimeType);
            }
        }

        public static void RunIndexHooks (ref string fullPath)
        {
            // is it a video that is not in the mkv file format?
            if (Video.IsValidFile (fullPath: fullPath) && Path.GetExtension (fullPath) != ".mkv") {
                // rename the video file to a file name indicating a raw format
                string oldPath = fullPath;
                string newPath = NamingUtilities.MakeRawFilename (fullPath);
                FileHooks.RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);

                /*
                // convert the video file
                string outputPath;
                if (VideoLibrary.Instance.EncodeMatroska (fullPath: fullPath, outputPath: out outputPath, encoding: VideoEncoding.COPY)) {
                    fullPath = outputPath;
                }
                */
            }

            try {
                // is it a video file in mkv format that is larger than 100MB?
                if (Path.GetExtension (fullPath) == ".mkv" && new FileInfo (fullPath).Length > 1024 * 1024 * 100) {
                    // split the video file
                    string outputPath;
                    if (VideoLibrary.Instance.SplitMatroska (fullPath: fullPath, outputPath: out outputPath)) {
                        fullPath = outputPath;
                    }
                }
            } catch (Exception ex) {
                Log.Error (ex);
            }
        }

        protected override void SerializeInternal (Dictionary<string, string> dict)
        {
        }

        protected override void DeserializeInternal (Dictionary<string, string> dict)
        {
        }
    }
}


﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Media.Files;
using Shell.Common.IO;

namespace Shell.Media.Content
{
    public class Video : Medium
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
            { new [] { "video/mp4" }, new [] { ".mp4" } },
            { new [] { "video/x-flv" }, new [] { ".flv" } },
            { new [] { "video/x-msvideo" }, new [] { ".avi" } },
            { new [] { "video/x-matroska" }, new [] { ".mkv" } },
            { new [] { "video/webm" }, new [] { ".webm" } },
            { new [] { "video/mpeg" }, new [] { ".mpg" } },
            { new [] { "video/ogg" }, new [] { ".ogv" } },
            { new [] { "video/x-ms-wmv" }, new [] { ".wmv" } },
            { new [] { "video/3gpp" }, new [] { ".3gp" } }
        };

        public static readonly string TYPE = "video";

        public override string Type { get { return TYPE; } }

        public Video (HexString hash)
            : base (hash)
        {
        }

        public Video (string fullPath)
            : base (fullPath: fullPath)
        {
        }

        public static bool IsValidFile (string fullPath)
        {
            return MediaShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void Index (string fullPath)
        {
        }

        public override bool IsCompletelyIndexed {
            get {
                return true;
            }
        }

        public static void RunIndexHooks (ref string fullPath)
        {
            // is it a video that is not in the mkv file format?
            if (Video.IsValidFile (fullPath: fullPath) && Path.GetExtension (fullPath) != ".mkv") {
                // convert the video file
                string outputPath;
                if (VideoLibrary.Instance.ConvertVideoToMatroska (fullPath: fullPath, outputPath: out outputPath)) {
                    fullPath = outputPath;
                }
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

        public override Dictionary<string, string> Serialize ()
        {
            return new Dictionary<string, string> ();
        }

        public override void Deserialize (Dictionary<string, string> dict)
        {
        }
    }
}

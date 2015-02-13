using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Media.Database;
using Shell.Media.Files;

namespace Shell.Media.Content
{
    public class Audio : MediaFile
    {
        public static HashSet<string> FILE_ENDINGS = new []{ ".mp3", ".wav", ".ogg", ".aac", ".m4a" }.ToHashSet ();

        public static Dictionary<string[],string[]> MIME_TYPES = new Dictionary<string[],string[]> () {
            { new [] { "audio/mpeg" }, new [] { ".mp3" } },
            { new [] { "audio/x-wav" }, new [] { ".wav" } },
            { new [] { "audio/ogg" }, new [] { ".ogg" } },
            { new [] { "audio/x-ms-wma" }, new [] { ".wma" } },
            { new [] { "audio/flac" }, new [] { ".flac" } }
        };

        public override DateTime? PreferredTimestampInternal { get { return null; } }

        public Audio (string fullPath, MediaDatabase database)
            : base (fullPath, database)
        {
        }

        public Audio ()
        {
        }

        public static new bool IsValidFile (string fullPath)
        {
            return MediaShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void Index ()
        {
        }

        public override bool IsCompletelyIndexed {
            get {
                return true;
            }
        }

        public static void RunIndexHooks (ref string fullPath)
        {
        }

        protected override void SerializeInternal (Dictionary<string, string> dict)
        {
        }

        protected override void DeserializeInternal (Dictionary<string, string> dict)
        {
        }
    }
}


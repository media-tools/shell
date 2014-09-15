﻿using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Picture : Medium
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".png", ".jpg", ".gif", ".jpeg" }.ToHashSet ();

        public static readonly string TYPE = "picture";

        public override string Type { get { return TYPE; } }

        public Picture (HexString hash)
            : base (hash)
        {
        }

        public Picture (string fullPath)
            : base (fullPath: fullPath)
        {
        }

        public static bool IsValidFile (string fullPath)
        {
            return PictureShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
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


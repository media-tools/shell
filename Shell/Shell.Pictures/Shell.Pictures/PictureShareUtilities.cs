using System;
using System.IO;
using System.Collections.Generic;

namespace Shell.Pictures
{
    public static class PictureShareUtilities
    {
        public static bool IsValidFile (string fullPath, HashSet<string> fileEndings)
        {
            return fileEndings.Contains (Path.GetExtension (fullPath).ToLower ());
        }
    }
}


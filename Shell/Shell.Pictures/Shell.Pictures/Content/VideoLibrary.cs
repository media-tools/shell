using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public sealed class VideoLibrary : Library
    {
        public VideoLibrary ()
        {
            ConfigName = "Pictures";
        }

        private static VideoLibrary _instance;

        public static VideoLibrary Instance { get { return _instance = _instance ?? new VideoLibrary (); } }

        public bool ConvertVideoToMatroska (string fullPath, out string outputPath)
        {
            try {
                string oldFullPath = fullPath;
                string newFullPath = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + ".mkv";

                if (!File.Exists (newFullPath)) {
                    long oldFileSize = new FileInfo (oldFullPath).Length;
                    int crf = oldFileSize < 50 * 1000000 ? 18 : oldFileSize > 200 * 1000000 ? 21 : 20;
                    string script = "export tempfile=$(mktemp --suffix .mkv) ;" +
                                    "rm -f '" + newFullPath + "' \"${tempfile}\" && " +
                                    "nice -n 19 ffmpeg -i '" + oldFullPath + "' -c:v libx264 -preset slower -crf " + crf + " \"${tempfile}\" &&" +
                                    "mv \"${tempfile}\" '" + newFullPath + "' &&" +
                                    "rm '" + oldFullPath + "' ;" +
                                    "rm -f \"${tempfile}\" ";

                    bool success = true;
                    Action<string> receiveOutput = line => {
                        if (line.ToLower ().Contains ("error")) {
                            success = false;
                            Log.Error ("Matroska Convert Error: ", line);
                        }
                    };

                    fs.Runtime.WriteAllText (path: "run4.sh", contents: script);
                    fs.Runtime.ExecuteScript (path: "run4.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

                    if (success) {
                        outputPath = newFullPath;
                    } else {
                        outputPath = null;
                    }

                    return success;
                }
            } catch (Exception ex) {
                Log.Error ("Error in ConvertVideoToMatroska:");
                Log.Error (ex);
            }
            outputPath = null;
            return false;
        }
    }
}

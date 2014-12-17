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
            Log.Message ();
            try {
                string oldFullPath = fullPath;
                string newFullPath = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + ".mkv";

                if (!File.Exists (newFullPath)) {
                    string ffmpegCommand = "ffmpeg";
                    string ffmpegParams = string.Empty;

                    if (Path.GetFileNameWithoutExtension (fullPath).Contains ("VID_")) {
                        ffmpegParams = " -vcodec copy -acodec copy ";
                    } else if (fullPath.Contains ("Music") || fullPath.Contains ("Musik")) {
                        ffmpegCommand = "x265-ffmpeg";
                        ffmpegParams = " -c:v hevc -c:a libfaac -preset slower ";
                    } else {
                        long oldFileSize = new FileInfo (oldFullPath).Length;
                        int crf = oldFileSize < 50 * 1000000 ? 18 : oldFileSize > 200 * 1000000 ? 21 : 20;
                        ffmpegParams = " -c:v libx264 -preset slower -crf " + crf + " ";
                    }

                    string script = "export tempfile=$(mktemp --suffix .mkv) ;" +
                                    "rm -f " + newFullPath.SingleQuoteShell () + " \"${tempfile}\" && " +
                                    "nice -n 19 " + ffmpegCommand + " -i " + oldFullPath.SingleQuoteShell () + " " + ffmpegParams + " \"${tempfile}\" && " +
                                    "mv \"${tempfile}\" " + newFullPath.SingleQuoteShell () + " && " +
                                    "rm " + oldFullPath.SingleQuoteShell () + " ;" +
                                    "rm -f \"${tempfile}\" ";

                    bool success = true;
                    Action<string> receiveOutput = line => {
                        if (line.ToLower ().Contains ("error")) {
                            success = false;
                            Log.Error ("Matroska Convert Error: ", line);
                            Log.Error ("Script:");
                            Log.Indent++;
                            Log.Error (script);
                            Log.Indent--;
                        }
                    };

                    fs.Runtime.WriteAllText (path: "run4.sh", contents: script);
                    fs.Runtime.ExecuteScript (path: "run4.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

                    if (!success && File.Exists (newFullPath) && !File.Exists (oldFullPath)) {
                        Log.Error ("New video files exists and the old one doesn't; let's assume it worked!");
                        success = true;
                    }

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

        public bool SplitMatroska (string fullPath, out string outputPath)
        {
            Log.Message ();
            try {
                string oldFullPath = fullPath;
                string newFullPathTemplate = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + "-splitted.mkv";
                string firstPart = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + "-splitted-001.mkv";

                if (!oldFullPath.Contains ("-splitted") && !File.Exists (firstPart)) {
                    int partSize = 1024 * 1024 * 95;
                    string script = "LC_ALL=C mkvmerge --split size:" + partSize + " --compression 0:none --compression 1:none --clusters-in-meta-seek -o "
                                    + newFullPathTemplate.SingleQuoteShell () + " "
                                    + oldFullPath.SingleQuoteShell () + " && " +
                                    "rm " + oldFullPath.SingleQuoteShell ();

                    bool success = true;
                    Action<string> receiveOutput = line => {
                        if (line.ToLower ().Contains ("error")) {
                            success = false;
                            Log.Error ("Matroska Convert Error: ", line);
                            Log.Error ("Script:");
                            Log.Indent++;
                            Log.Error (script);
                            Log.Indent--;
                        }
                    };

                    fs.Runtime.WriteAllText (path: "run5.sh", contents: script);
                    fs.Runtime.ExecuteScript (path: "run5.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

                    if (!success && File.Exists (firstPart) && !File.Exists (oldFullPath)) {
                        Log.Error ("New video files exists and the old one doesn't; let's assume it worked!");
                        success = true;
                    } else if (success && File.Exists (oldFullPath) && !File.Exists (firstPart)) {
                        Log.Error ("New viceo file doesn't exist. It doesn't seem to have worked....");
                        success = false;
                    }

                    if (success) {
                        outputPath = firstPart;
                    } else {
                        outputPath = null;
                    }

                    return success;
                }
            } catch (Exception ex) {
                Log.Error ("Error in SplitMatroska:");
                Log.Error (ex);
            }
            outputPath = null;
            return false;
        }
    }
}

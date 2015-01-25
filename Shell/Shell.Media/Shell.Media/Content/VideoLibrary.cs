using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;
using Shell.Media.Files;
using System.Linq;

namespace Shell.Media.Content
{
    public sealed class VideoLibrary : Library
    {
        public VideoLibrary ()
        {
            ConfigName = NamespacePictures.CONFIG_NAME;
        }

        private static VideoLibrary _instance;

        public static VideoLibrary Instance { get { return _instance = _instance ?? new VideoLibrary (); } }

        private Random random = new Random ();

        public bool EncodeMatroska (string fullPath, out string outputPath, VideoEncoding encoding, int crf = -1)
        {
            string oldFullPath = fullPath;
            string newFullPath = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + ".mkv";

            if (EncodeMatroska (sourceFullPath: oldFullPath, destinationFullPath: newFullPath, encoding: encoding, crf: crf)) {
                outputPath = newFullPath;
            } else {
                outputPath = null;
            }

            return outputPath != null;
        }

        public bool EncodeMatroska (string sourceFullPath, string destinationFullPath, VideoEncoding encoding, int crf = -1)
        {
            try {
                if (!File.Exists (destinationFullPath)) {
                    string ffmpegCommand = "x265-ffmpeg";
                    string ffmpegParams = string.Empty;

                    switch (encoding) {
                    case VideoEncoding.H265:
                        //ffmpegParams = " -c:v hevc -c:a libfaac -preset veryslow -strict experimental -pix_fmt yuv420p ";
                        ffmpegParams = " -c:a libfdk_aac -b:a 256k -c:v hevc -preset veryslow -strict experimental -pix_fmt yuv420p ";
                        break;

                    case VideoEncoding.H264:
                        long oldFileSize = new FileInfo (sourceFullPath).Length;
                        if (crf == -1) {
                            crf = oldFileSize < 50 * 1000000 ? 22 : 25;
                        }
                        //ffmpegParams = " -c:v libx264 -preset veryslow -crf " + crf + " -strict experimental -pix_fmt yuv420p ";
                        ffmpegParams = " -c:a libfdk_aac -b:a 256k -c:v libx264 -preset veryslow -crf " + crf + " -strict experimental -pix_fmt yuv420p ";
                        break;

                    case VideoEncoding.COPY:
                        ffmpegParams = " -vcodec copy -acodec copy ";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException ("Invalid conversion parameters!");
                    }

                    string script = "export tempfile=$(mktemp --suffix .mkv) ;" +
                                    "rm -f " + destinationFullPath.SingleQuoteShell () + " \"${tempfile}\" && " +
                                    "nice -n 19 " + ffmpegCommand + " -i " + sourceFullPath.SingleQuoteShell () + " " + ffmpegParams + " \"${tempfile}\" && " +
                                    "mv \"${tempfile}\" " + destinationFullPath.SingleQuoteShell () + " && " +
                                    "rm " + sourceFullPath.SingleQuoteShell () + " ;" +
                                    "rm -f \"${tempfile}\" ";

                    bool success = true;
                    Action<string> receiveOutput = line => {
                        if (line.ToLower ().Contains ("error")) {
                            success = false;
                            Log.Error ("Error in EncodeMatroska: ", line);
                            Log.Error ("Script:");
                            Log.Indent++;
                            Log.Error (script);
                            Log.Indent--;
                        }
                    };

                    int randInt = random.Next ();
                    fs.Runtime.WriteAllText (path: "run4" + randInt + ".sh", contents: script);
                    fs.Runtime.ExecuteScript (path: "run4" + randInt + ".sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

                    if (!success && File.Exists (destinationFullPath) && !File.Exists (sourceFullPath)) {
                        Log.Error ("New video files exists and the old one doesn't; let's assume it worked!");
                        success = true;
                    }
                    
                    return success;
                }
            } catch (Exception ex) {
                Log.Error ("Error in EncodeMatroska:");
                Log.Error (ex);
            }
            return false;
        }

        public bool SplitMatroska (string fullPath, out string outputPath)
        {
            try {
                string oldFullPath = fullPath;
                string newFullPathTemplate = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + "-splitted.mkv";
                string firstPart = Path.GetDirectoryName (fullPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (fullPath) + "-splitted-001.mkv";
                long originalFileSize = new FileInfo (fullPath).Length;

                if (!oldFullPath.Contains ("-splitted") && !File.Exists (firstPart)) {
                    int partSize = 1024 * 1024 * 80;
                    string script = "LC_ALL=C mkvmerge --split size:" + partSize + " --compression 0:none --compression 1:none --clusters-in-meta-seek -o "
                                    + newFullPathTemplate.SingleQuoteShell () + " "
                                    + oldFullPath.SingleQuoteShell () + " && " +
                                    "rm " + oldFullPath.SingleQuoteShell ();

                    bool success = true;
                    Action<string> receiveOutput = line => {
                        if (line.ToLower ().Contains ("error")) {
                            success = false;
                            Log.Error ("Error in SplitMatroska: ", line);
                            Log.Error ("Script:");
                            Log.Indent++;
                            Log.Error (script);
                            Log.Indent--;
                        }
                    };

                    int randInt = random.Next ();
                    fs.Runtime.WriteAllText (path: "run5" + randInt + ".sh", contents: script);
                    fs.Runtime.ExecuteScript (path: "run5" + randInt + ".sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

                    if (!success && File.Exists (firstPart) && !File.Exists (oldFullPath)) {
                        Log.Error ("New video files exists and the old one doesn't; let's assume it worked!");
                        success = true;
                    } else if (success && File.Exists (oldFullPath) && !File.Exists (firstPart)) {
                        Log.Error ("New video file doesn't exist. It doesn't seem to have worked....");
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

        public bool MergeMatroska (string outputPath, string[] inputPaths)
        {
            try {
                if (!File.Exists (outputPath) && inputPaths.Length >= 2) {
                    string script = "LC_ALL=C mkvmerge --compression 0:none --compression 1:none --clusters-in-meta-seek -o "
                                    + outputPath.SingleQuoteShell () + " "
                                    + inputPaths.First ().SingleQuoteShell () + " "
                                    + string.Join (" ", inputPaths.Skip (1).Select (i => "+" + i.SingleQuoteShell ()))
                                    + " && "
                                    + "rm " + string.Join (" ", inputPaths.Select (i => i.SingleQuoteShell ()));

                    bool success = true;
                    Action<string> receiveOutput = line => {
                        if (line.ToLower ().Contains ("error") && !line.ToLower ().Contains ("keep that in mind")) {
                            success = false;
                            Log.Error ("Error in MergeMatroska: ", line);
                            Log.Error ("Script:");
                            Log.Indent++;
                            Log.Error (script);
                            Log.Indent--;
                        }
                    };

                    fs.Runtime.WriteAllText (path: "run6.sh", contents: script);
                    fs.Runtime.ExecuteScript (path: "run6.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

                    if (!success && !inputPaths.Any (i => File.Exists (i)) && File.Exists (outputPath)) {
                        Log.Error ("New video file exists and the old ones don't; let's assume it worked!");
                        success = true;
                    } else if (success && inputPaths.Any (i => File.Exists (i)) && !File.Exists (outputPath)) {
                        Log.Error ("New video file doesn't exist. It doesn't seem to have worked....");
                        success = false;
                    }

                    return success;
                }
            } catch (Exception ex) {
                Log.Error ("Error in MergeMatroska:");
                Log.Error (ex);
            }
            outputPath = null;
            return false;
        }
    }

    public enum VideoEncoding
    {
        COPY,
        H264,
        H265,
    }
}

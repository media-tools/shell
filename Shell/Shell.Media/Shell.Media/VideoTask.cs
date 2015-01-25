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
using Shell.Namespaces;

namespace Shell.Media
{
    public class VideoTask : MonoOptionsScriptTask, MainScriptTask
    {
        public VideoTask ()
        {
            Name = "Video";
            Options = new [] { "video" };
            ConfigName = NamespacePictures.CONFIG_NAME;

            Description = new [] {
                "Merge consecutive SJCAM video files",
                "Encode raw video files in H264",
            };
            Parameters = new [] {
                "sjcam-merge",
                "encode",
            };
            Methods = new Action[] {
                () => videoMerge (),
                () => videoEncode (),
            };
        }

        List<string> directories = new List<string> ();
        VideoEncoding? encoding;
        int? crf;
        bool dryRun = false;

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet
                .Add ("directory|dir=",
                "A directory is needed for most operations",
                option => directories.Add (option))
                .Add ("h264",
                "Use H264 video encoding",
                option => encoding = VideoEncoding.H264)
                .Add ("h265",
                "Use H265 video encoding",
                option => encoding = VideoEncoding.H265)
                .Add ("crf=",
                "Use the specified constant rate factor (CRF) for H264/H265",
                option => {
                    int _crf;
                    if (int.TryParse (option, out _crf)) {
                        crf = _crf;
                    } else {
                        Log.Error ("Invalid CRF: ", option);
                        crf = -1;
                    }
                })
                .Add ("dry-run",
                "Don't modify the file system",
                option => dryRun = option != null);
        }

        void videoMerge ()
        {
            string[] _directories = FileSystemUtilities.ToAbsolutePaths (directories);

            if (!_directories.Any ()) {
                Log.Error ("You have to set a directory.");
                return;
            }

            ActionCamUtilities acu = new ActionCamUtilities ();
            acu.VideoMerge (directories: _directories, dryRun: dryRun);
        }

        void videoEncode ()
        {
            string[] _directories = FileSystemUtilities.ToAbsolutePaths (directories);

            if (!_directories.Any ()) {
                Log.Error ("You have to set a directory.");
                return;
            }

            if (!encoding.HasValue) {
                Log.Error ("You have to set an encoding!");
                return;
            }

            if (!crf.HasValue) {
                Log.Error ("You have to set a CRF!");
                return;
            }

            VideoUtilities acu = new VideoUtilities ();
            acu.VideoEncodeDirectory (directories: _directories, encoding: encoding.Value, crf: crf.Value, dryRun: dryRun);
        }
    }
}


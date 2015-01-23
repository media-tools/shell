using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;
using System.Linq;

namespace Shell.Media
{
    public class ActionCamTask : MonoOptionsScriptTask, MainScriptTask
    {
        public ActionCamTask ()
        {
            Name = "ActionCam";
            Options = new [] { "action-cam" };
            ConfigName = NamespacePictures.CONFIG_NAME;

            Description = new [] {
                "Merge consecutive SJCAM video files",
            };
            Parameters = new [] {
                "video-merge",
            };
            Methods = new Action[] {
                () => videoMerge (),
            };
        }

        List<string> directories = new List<string> ();
        bool dryRun = false;

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet
                .Add ("directory|dir=",
                "A directory is needed for most operations",
                option => directories.Add (option))
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
    }
}


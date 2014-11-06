using System;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Common.IO
{
    public class ProgressBar : Library
    {
        public string Identifier { get; private set; }

        public string Description { get; private set; }

        private float estimatedMaxValue;
        private float currentValue;
        private static ConfigFile progressCache;
        private int skipStepsForPrinting;

        public static int MAX_WIDTH = 150;

        // ETA
        private DateTime? etaStartTime;
        private float etaStartProgress;
        private float etaCurrentProgress;

        public ProgressBar (string identifier, string description)
        {
            Identifier = identifier;
            Description = description;
            Console.CursorVisible = false;
            

            if (progressCache == null) {
                FileSystem fsCache = new FileSystem (library: this, type: FileSystemType.Runtime);
                progressCache = fsCache.OpenConfigFile (name: "progress_cache.ini");
            }
            estimatedMaxValue = progressCache ["MaxValue", Identifier, 1];
            skipStepsForPrinting = (int)(estimatedMaxValue / 1000f);
            currentValue = 0;
        }

        public void Finish ()
        {
            int left = Console.CursorLeft;
            Console.Write (String.Concat (Enumerable.Repeat (" ", MAX_WIDTH)));
            Console.CursorLeft = left;
            Console.CursorVisible = true;
            progressCache ["MaxValue", Identifier, 1] = currentValue;
        }

        public void Print (float current, float min, float max, string currentDescription, bool showETA, bool updateETA)
        {
            // show eta?
            string etaString = string.Empty;
            if (showETA) {
                if (etaStartTime.HasValue) {
                    if (updateETA) {
                        etaCurrentProgress = current;
                    }
                    DateTime etaCurrentTime = DateTime.UtcNow;
                    TimeSpan etaTimeSpan = (etaCurrentTime - etaStartTime.Value).Multiply ((max - etaStartProgress) / (etaCurrentProgress - etaStartProgress));
                    etaString = string.Format (" (ETA {0:hh\\:mm\\:ss})", etaTimeSpan);
                } else {
                    if (updateETA) {
                        etaStartTime = DateTime.UtcNow;
                        etaStartProgress = current;
                    }
                }
            }

            // compute progress
            float progress = (current - min) / (max - min);

            // print it
            int left = Console.CursorLeft;
            string line = string.Format ("{0} {1:P2} {2}{3}", Description, progress, currentDescription, etaString);
            if (line.Length > MAX_WIDTH) {
                line = line.Substring (0, MAX_WIDTH);
            }
            Console.Write (line + String.Concat (Enumerable.Repeat (" ", Math.Max (0, MAX_WIDTH - line.Length))));
            Console.CursorLeft = left;
            Console.Out.Flush ();
            currentValue = current;
        }

        public void Print (float current, string currentDescription = "", bool showETA = false, bool updateETA = true)
        {
            if (current >= estimatedMaxValue) {
                estimatedMaxValue *= 1.5f;
                progressCache ["MaxValue", Identifier, 1] = estimatedMaxValue;
            }
            Print (current: current, min: 0, max: estimatedMaxValue, currentDescription: currentDescription, showETA: showETA, updateETA: updateETA);
        }

        public void Next (bool printAlways = false, string currentDescription = "")
        {
            currentValue++;
            if (printAlways || (currentValue % skipStepsForPrinting == 0)) {
                Print (currentValue, currentDescription);
            }
        }
    }
}


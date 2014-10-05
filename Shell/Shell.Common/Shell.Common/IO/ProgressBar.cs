using System;
using Shell.Common.IO;
using System.Linq;

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
            Console.Write (String.Concat (Enumerable.Repeat (" ", 79)));
            Console.CursorLeft = left;
            Console.CursorVisible = true;
            progressCache ["MaxValue", Identifier, 1] = currentValue;
        }

		public void Print (float current, float min, float max, string currentDescription = "")
        {
            float progress = (current - min) / (max - min);
            int left = Console.CursorLeft;
			Console.Write (string.Format ("{0} {1:P2} {2}                    ", Description, progress, currentDescription));
            Console.CursorLeft = left;
            currentValue = current;
        }

		public void Print (float current, string currentDescription = "")
        {
            if (current >= estimatedMaxValue) {
                estimatedMaxValue *= 1.5f;
                progressCache ["MaxValue", Identifier, 1] = estimatedMaxValue;
            }
			Print (current: current, min: 0, max: estimatedMaxValue, currentDescription: currentDescription);
        }

		public void Next (bool printAlways = false, string currentDescription = "")
        {
            currentValue ++;
			if (printAlways || (currentValue % skipStepsForPrinting == 0)) {
				Print (currentValue, currentDescription);
            }
        }
    }
}


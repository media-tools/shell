using System;
using System.Collections.Generic;
using Control.Common;

namespace Control.Series
{
    public static class SeriesLibrary
    {
        public static List<string> VideoFiles = new List<string> ();

        public static void ReadCache ()
        {

        }

        public static void Scan (FileSystem fsRuntime)
        {
            string script = "locate --regex \"(mp4|mkv)$\" \n";
            fsRuntime.WriteAllText (path: "scan.sh", contents: script);
            Action<string> receiveOutput = line => {
                line.Trim ('\r', '\n');
                AddVideoFile (filepath: line);
            };
            fsRuntime.ExecuteScript (path: "scan.sh", receiveOutput: receiveOutput, verbose: false);
        }

        private static void AddVideoFile (string filepath)
        {
            if (EpisodeFile.IsEpisodeFile (filepath)) {
                EpisodeFile episodeFile = new EpisodeFile (filepath: filepath);
                Log.Message ("Found episode: ", episodeFile);
            }
        }
    }
}


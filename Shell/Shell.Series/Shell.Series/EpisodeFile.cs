using System;
using System.Collections.Generic;
using Shell.Common;
using System.IO;
using System.Text.RegularExpressions;
using Shell.Common.IO;

namespace Shell.Series
{
    public class EpisodeFile
    {
        private EpisodeInfo info;

        public EpisodeFile (string filepath)
        {
            Match (filepath: filepath, info: out info);
        }

        public static bool IsEpisodeFile (string filepath)
        {
            EpisodeInfo useless;
            return Match (filepath: filepath, info: out useless);
        }

        public static bool Match (string filepath, out EpisodeInfo info)
        {
            string[] patterns = new string[] {
                @"^(?<series>.*?)\.S(?<season>[0-9]*?)E(?<episode>[0-9]*?)\.(?<stuff>.*)$",
            };
            string filename = Path.GetFileNameWithoutExtension (filepath);
            foreach (string pattern in patterns) {
                foreach (Match match in Regex.Matches(filename, pattern, RegexOptions.IgnoreCase)) {
                    try {
                        info = new EpisodeInfo (
                            series: match.Groups ["series"].Value,
                            season: int.Parse (match.Groups ["season"].Value),
                            episode: int.Parse (match.Groups ["episode"].Value),
                            suffix: match.Groups ["stuff"].Value
                        );
                        return true;
                    } catch (FormatException ex) {
                        Log.Debug (ex);
                    }
                }
            }
            info = null;
            return false;
        }

        public override string ToString() {
            return info.ToString ();
        }
    }
}



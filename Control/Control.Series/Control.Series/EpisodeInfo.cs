using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Control.Series
{
    public class EpisodeInfo
    {
        public string Series { get; private set; }

        public int Season { get; private set; }

        public int Episode { get; private set; }

        public string Title { get; private set; }

        public string ReleaseGroup { get; private set; }

        public int Resolution { get; private set; }

        public string Codec { get; private set; }

        public bool IsRepack { get; private set; }

        public EpisodeInfo (string series, int season, int episode, string suffix)
        {
            Series = series;
            Season = season;
            Episode = episode;
            string releaseGroup;
            extractReleaseGroup (ref suffix, out releaseGroup);
            int resolution;
            extractResolution (ref suffix, out resolution);
            string codec;
            extractCodec (ref suffix, out codec);
            bool isRepack;
            extractIsRepack (ref suffix, out isRepack);
            removeBullshit (ref suffix);
            ReleaseGroup = releaseGroup;
            Resolution = resolution;
            Codec = codec;
            IsRepack = isRepack;
            Title = suffix;
        }

        private void extractReleaseGroup (ref string suffix, out string releaseGroup)
        {
            releaseGroup = "";
            string[] patterns = new string[] {
                @"[-](?<group>[A-Za-z0-9]+)(?<after>\.|$)",
                @"\.(?<group>[A-Za-z0-9]+)$",
            };
            foreach (string pattern in patterns) {
                foreach (Match match in Regex.Matches(suffix, pattern, RegexOptions.IgnoreCase)) {
                    releaseGroup = match.Groups ["group"].Value;
                    suffix = Regex.Replace (suffix, pattern, match.Groups ["after"].Value, RegexOptions.IgnoreCase);
                    return;
                }
            }
        }

        private void extractResolution (ref string suffix, out int resolution)
        {
            resolution = 400;
            string[] patterns = new string[] {
                @"\.(?<resolution>720|1080)p",
                @"(?<resolution>720|1080)p\.",
            };
            foreach (string pattern in patterns) {
                foreach (Match match in Regex.Matches(suffix, pattern, RegexOptions.IgnoreCase)) {
                    resolution = int.Parse (match.Groups ["resolution"].Value);
                    suffix = Regex.Replace (suffix, pattern, "", RegexOptions.IgnoreCase);
                    return;
                }
            }
        }

        private void extractCodec (ref string suffix, out string codec)
        {
            codec = "";
            string[] patterns = new string[] {
                @"\.(?<codec>H\.264|x264|h264)",
                @"(?<codec>H\.264|x264|h264)\.",
            };
            foreach (string pattern in patterns) {
                foreach (Match match in Regex.Matches(suffix, pattern, RegexOptions.IgnoreCase)) {
                    codec = "x264";
                    suffix = Regex.Replace (suffix, pattern, "", RegexOptions.IgnoreCase);
                    return;
                }
            }
        }

        private void extractIsRepack (ref string suffix, out bool isRepack)
        {
            isRepack = false;
            string[] patterns = new string[] {
                @"\.(?<codec>repack|proper)",
                @"(?<codec>repack|proper)\.",
                @"(?<codec>repack|proper)",
            };
            foreach (string pattern in patterns) {
                foreach (Match match in Regex.Matches(suffix, pattern, RegexOptions.IgnoreCase)) {
                    isRepack = true;
                    suffix = Regex.Replace (suffix, pattern, "", RegexOptions.IgnoreCase);
                    return;
                }
            }
        }

        private void removeBullshit (ref string suffix)
        {
            removeBullshit (ref suffix, "DD5.1");
            removeBullshit (ref suffix, "WEB.?DL");
            removeBullshit (ref suffix, "HDTV");
        }

        private void removeBullshit (ref string suffix, string pattern)
        {
            suffix = Regex.Replace (suffix, @"\." + pattern, "", RegexOptions.IgnoreCase);
            suffix = Regex.Replace (suffix, pattern + @"\.", "", RegexOptions.IgnoreCase);
            suffix = Regex.Replace (suffix, pattern, "", RegexOptions.IgnoreCase);
        }

        public override string ToString ()
        {
            return string.Join ("", "Series: ", Series, ", Season: ", Season, ", Episode: ", Episode, ", Group: ", ReleaseGroup, ", Resolution: ", Resolution, "p, Codec: ", Codec, ", Title: ", Title);
        }
    }
}


using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.Series
{
    public class SeriesTask : ScriptTask, MainScriptTask
    {
        public SeriesTask ()
        {
            Name = "Series";
            Description = new [] { "Update series and video files", "Search for series and video files" };
            Options = new [] { "series" };
            ParameterSyntax = new [] { "update", "scan" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "update":
                    update ();
                    break;
                case "scan":
                    scan ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void update ()
        {
        }

        void scan ()
        {
            SeriesLibrary.Scan (fsRuntime : fs.Runtime);
        }
    }
}


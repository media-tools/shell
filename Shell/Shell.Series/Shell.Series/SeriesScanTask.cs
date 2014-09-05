using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.Series
{
    public class SeriesScanTask : Task, MainTask
    {
        public SeriesScanTask ()
        {
            Name = "SeriesScan";
            Description = "Search for series and video files";
            Options = new string[] { "series-scan" };
        }

        protected override void InternalRun (string[] args)
        {
            SeriesLibrary.Scan (fsRuntime : fs.Runtime);
        }
    }
}


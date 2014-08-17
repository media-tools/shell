using System;
using Control.Common;
using Control.Common.Tasks;

namespace Control.Series
{
    public class SeriesTask : Task, MainTask
    {
        public SeriesTask ()
        {
            Name = "Series";
            Description = "Update series and video files";
            Options = new string[] { "series" };
        }

        protected override void InternalRun (string[] args)
        {
        }
    }
}


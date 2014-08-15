using System;
using Control.Common;

namespace Control.Series
{
    public class SeriesTask : Task
    {
        public SeriesTask ()
        {
            Name = "Series";
            Description = "Update series and video files";
            Options = new string[] { "series" };
        }
    }
}


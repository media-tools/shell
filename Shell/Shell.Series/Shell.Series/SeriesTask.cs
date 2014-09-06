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
            Description = "Update series and video files";
            Options = new string[] { "series" };
        }

        protected override void InternalRun (string[] args)
        {
        }
    }
}


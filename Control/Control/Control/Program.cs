using System;
using Control.Common;
using Control.MailSync;
using Control.Series;

namespace Control
{
    public class MainClass
    {
        private static Task[] mainTasks = new Task[] {
            new MailSyncTask(),
            new SeriesTask()
        };

        public static void Main (string[] args)
        {
            Log.Init (program: "control", version: Commons.VERSION_STR);
            new TaskProgram (mainTasks).Main (args);
        }
    }
}

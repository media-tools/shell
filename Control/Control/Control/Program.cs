using System;
using Control.Common;
using Control.MailSync;
using Control.Series;
using Control.Git;

namespace Control
{
    public class MainClass
    {
        private static Task[] mainTasks = new Task[] {
            new MailSyncTask(),
            new SeriesTask(),
            new GitTask()
        };

        public static void Main (string[] args)
        {
            Log.Init (program: "control", version: Commons.VERSION_STR);

            Program.HooksBeforeTask += GitHook.CommitHook;

            new Program (mainTasks).Main (args);
        }
    }
}

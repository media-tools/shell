using System;
using Control.Common;
using Control.MailSync;
using Control.Series;
using Control.Git;
using Control.Logging;

namespace Control
{
    public class MainClass
    {
        private static Task[] mainTasks = new Task[] {
            new MailSyncTask(),
            new MailFilterTask(),
            new MailDedupTask(),
            new MailAllTask(),
            new SeriesTask(),
            new SeriesScanTask(),
            new GitTask(),
            new GitCommitTask(),
            new InstallTask(),
            new LogTask(),
        };

        public static void Main (string[] args)
        {
            Log.Init (program: "control", version: Commons.VERSION_STR);

            Program.HooksBeforeTask += GitHook.CommitHook;

            try {
                new Program (mainTasks).Main (args);
            } catch (Exception ex) {
                Log.Error (ex);
            }
        }
    }
}

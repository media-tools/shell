using System;
using Control.Common;
using System.Linq;

namespace Control.Git
{
    public static class GitHook
    {
        private static GitTask task;

        public static void Execute (params string[] args)
        {
            task = task ?? new GitTask();
            FileSystem fsConfig = new FileSystem (FileSystemType.Config);
            FileSystem fsRuntime = new FileSystem (task, FileSystemType.Runtime);
            string script = "";
            script += "cd " + fsConfig.RootDirectory + "\n";
            Log.Message ("Check if git repository exists in: " + (fsConfig.DirectoryExists (".git") ? "Yes" : "No"));
            if (!fsConfig.DirectoryExists (".git")) {
                script += "git init\n";
            }
            script += "git add --all\n";
            script += "git " + string.Join (" ", from arg in args select "\"" + arg + "\"") + "\n";
            fsRuntime.WriteAllText (path: "git.sh", contents: script);
            fsRuntime.RequirePackages ("git");
            fsRuntime.ExecuteScript (path: "git.sh");
        }

        public static void Commit ()
        {
            string msg = "committed by: " + string.Join (" ", Environment.GetCommandLineArgs ()).Replace ("\"", "");
            Execute ("commit", "-a", "-m", msg);
        }

        public static void CommitHook (Task task)
        {
            if (!(task is GitTask)) {
                Commit ();
            }
        }
    }
}


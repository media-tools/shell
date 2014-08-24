using System;
using Control.Common;
using System.Linq;
using Control.Common.IO;
using System.IO;
using Control.Common.Util;

namespace Control.Git
{
    public static class GitLibrary
    {
        private static GitTask task;

        public static void Execute (params string[] args)
        {
            Execute (verbose: true, args: args);
        }

        public static void Execute (string[] args, bool verbose = true, string rootDirectory = null)
        {
            Execute (new string[][] { args }, verbose: verbose, rootDirectory: rootDirectory);
        }

        public static void Execute (string[][] commands, bool verbose = true, string rootDirectory = null)
        {
            task = task ?? new GitTask ();
            FileSystem fsRuntime = new FileSystem (task, FileSystemType.Runtime);
            FileSystem fsConfig = new FileSystem (FileSystemType.Config);
            rootDirectory = rootDirectory ?? fsConfig.RootDirectory;
            string script = "";
            script += "cd " + rootDirectory + "\n";
            bool isGitRepo = Directory.Exists (rootDirectory + SystemInfo.PathSeparator + ".git");
            Log.MessageLog ("Check if git repository exists in: ", rootDirectory, ": ", (isGitRepo ? "Yes" : "No"));
            if (!isGitRepo) {
                script += "git init\n";
            }
            script += "git add --all\n";
            foreach (string[] args in commands) {
                script += "git " + string.Join (" ", from arg in args select "\"" + arg + "\"") + "\n";
            }
            fsRuntime.WriteAllText (path: "git.sh", contents: script);
            fsRuntime.RequirePackages ("git");
            fsRuntime.ExecuteScript (path: "git.sh", verbose: verbose, debug: false);
        }

        public static void Commit ()
        {
            string msg = "committed by: " + string.Join (" ", Environment.GetCommandLineArgs ()).Replace ("\"", "");
            Execute (args: new [] { "commit", "-a", "-m", msg }, verbose: false);
        }
    }
}


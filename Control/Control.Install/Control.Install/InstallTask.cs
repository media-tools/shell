using System;
using Control.Common;

namespace Control.Git
{
    public class InstallTask : Task
    {
        public InstallTask ()
        {
            Name = "Install";
            Description = "Install this shit";
            Options = new string[] { "install" };
        }

        protected override void InternalRun (string[] args)
        {
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
    }
}


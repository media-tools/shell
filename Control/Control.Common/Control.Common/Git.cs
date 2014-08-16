using System;

namespace Control.Common
{
    public static class Git
    {
        public static void Commit ()
        {
            FileSystem fsConfig = new FileSystem (FileSystemType.Config);
            FileSystem fsRuntime = new FileSystem (FileSystemType.Runtime);
            string script = "";
            script += "cd " + fsConfig.RootDirectory + "\n";
            Log.Message ("Check if git repository exists in: " + (fsConfig.DirectoryExists (".git") ? "Yes" : "No"));
            if (!fsConfig.DirectoryExists (".git")) {
                script += "git init\n";
            }
            script += "git add --all\n";
            string msg = "committed by control: " + string.Join (" ", Environment.GetCommandLineArgs ()).Replace ("\"", "");
            script += "git commit -a -m \"" + msg + "\"\n";
            fsRuntime.WriteAllText (path: "git.sh", contents: script);
            fsRuntime.RequirePackages ("git");
            fsRuntime.ExecuteScript (path: "git.sh");
        }
    }
}


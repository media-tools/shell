using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.Git
{
    public class ConfigCommitTask : ScriptTask, MainScriptTask
    {
        public ConfigCommitTask ()
        {
            Name = "ConfigCommit";
            Description = "Commit changes in the config directory";
            Options = new string[] { "config-commit" };
        }

        protected override void InternalRun (string[] args)
        {
            fs.Runtime.RequirePackages ("git");

            GitLibrary.Commit ();
        }
    }
}


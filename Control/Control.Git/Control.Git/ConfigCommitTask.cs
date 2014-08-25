using System;
using Control.Common;
using Control.Common.Tasks;

namespace Control.Git
{
    public class ConfigCommitTask : Task, MainTask
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


using System;
using Control.Common;
using Control.Common.Tasks;

namespace Control.Git
{
    public class GitCommitTask : Task, MainTask
    {
        public GitCommitTask ()
        {
            Name = "GitCommit";
            Description = "Commit changes in the config directory";
            Options = new string[] { "git-commit" };
        }

        protected override void InternalRun (string[] args)
        {
            fs.Runtime.RequirePackages ("git");

            GitLibrary.Commit ();
        }
    }
}


using System;
using Control.Common;

namespace Control.Git
{
    public class GitCommitTask : Task
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

            GitHook.Commit ();
        }
    }
}


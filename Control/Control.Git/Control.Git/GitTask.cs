using System;
using Control.Common;

namespace Control.Git
{
    public class GitTask : Task
    {
        public GitTask ()
        {
            Name = "Git";
            Description = "Git";
            Options = new string[] { "git" };
        }

        protected override void InternalRun (string[] args)
        {
            fs.Runtime.RequirePackages ("git");

            if (args.Length == 0) {
                GitHook.Commit ();
            } else {
                GitHook.Execute (args);
            }
        }
    }
}


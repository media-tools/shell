using System;
using Control.Common;

namespace Control.Git
{
    public class GitTask : Task
    {
        public GitTask ()
        {
            Name = "Git";
            Description = "Execute git in the config directory";
            Options = new string[] { "git" };
            ParameterSyntax = "[GIT OPTIONS]";
        }

        protected override void InternalRun (string[] args)
        {
            fs.Runtime.RequirePackages ("git");

            GitHook.Execute (args);
        }
    }
}


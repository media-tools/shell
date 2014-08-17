using System;
using Control.Common;
using Control.Common.Tasks;

namespace Control.Git
{
    public class GitTask : Task, MainTask
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

            GitLibrary.Execute (args);
        }
    }
}


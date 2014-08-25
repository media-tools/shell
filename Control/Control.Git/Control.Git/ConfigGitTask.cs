using System;
using Control.Common;
using Control.Common.Tasks;

namespace Control.Git
{
    public class ConfigGitTask : Task, MainTask
    {
        public ConfigGitTask ()
        {
            Name = "ConfigGit";
            Description = "Execute git in the config directory";
            Options = new string[] { "config-git" };
            ParameterSyntax = "[GIT OPTIONS]";
        }

        protected override void InternalRun (string[] args)
        {
            fs.Runtime.RequirePackages ("git");

            GitLibrary.Execute (args);
        }
    }
}


using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.Git
{
    public class ConfigGitTask : ScriptTask, MainScriptTask
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


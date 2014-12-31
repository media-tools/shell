using System;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;

namespace Shell.Git
{
    public class ConfigGitTask : ScriptTask, MainScriptTask
    {
        public ConfigGitTask ()
        {
            Name = "Config";
            Description = new [] { "Execute git in the config directory", "Commit changes in the config directory" };
            Options = new [] { "config" };
            ParameterSyntax = new [] { "--git [GIT OPTIONS]", "--commit" };
            Log.DEBUG_ENABLED = true;
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "--git":
                    git (args.Skip (1).ToArray ());
                    break;
                case "--commit":
                    commit ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void git (string[] args)
        {
            fs.Runtime.RequirePackages ("git");
            GitLibrary.Execute (args);
        }

        void commit ()
        {
            fs.Runtime.RequirePackages ("git");
            GitLibrary.Commit ();
        }
    }
}


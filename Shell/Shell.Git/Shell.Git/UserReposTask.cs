using System;
using System.Linq;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.IO;
using System.Collections.Generic;

namespace Shell.Git
{
    public class UserReposTask : ScriptTask, MainScriptTask
    {
        private ConfigFile config;

        public UserReposTask ()
        {
            Name = "UserRepos";
            Description = "Commit changes in the user's selected repositories or modify the list of repositories";
            Options = new string[] { "user-repos", "ur" };
            ParameterSyntax = "commit | add | list | remove";
        }

        private HashSet<string> _repoPaths;

        private HashSet<string> RepoPaths {
            get {
                return _repoPaths = _repoPaths ?? new HashSet<string> (config ["UserRepos", "paths", ""].Split (new char[] {
                    ':',
                    ';'
                }, StringSplitOptions.RemoveEmptyEntries));
            }
            set {
                _repoPaths = value;
            }
        }

        private void SaveRepoPaths ()
        {
            RepoPaths = new HashSet<string> (from path in RepoPaths select path.TrimEnd (new char[] { '/', '\\' }));
            config ["UserRepos", "paths", ""] = string.Join (":", RepoPaths);
        }

        protected override void InternalRun (string[] args)
        {
            fs.Runtime.RequirePackages ("git");
            config = fs.Config.OpenConfigFile ("userrepos.ini");

            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "commit":
                    commit ();
                    break;
                case "add":
                    if (args.Length == 2) {
                        addRepo (args [1]);
                        Log.Message ("Added repository: " + args [1]);
                        list ();
                    } else {
                        Log.Error ("Invalid parameters. The second parameter has to be a path.");
                    }
                    break;
                case "remove":
                    if (args.Length == 2) {
                        removeRepo (args [1]);
                        Log.Message ("Added repository: " + args [1]);
                        list ();
                    } else {
                        Log.Error ("Invalid parameters. The second parameter has to be a path.");
                    }
                    break;
                case "list":

                    list ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        private void commit ()
        {
            if (RepoPaths.Count >= 1) {
                string msg = "committed by: " + string.Join (" ", Environment.GetCommandLineArgs ()).Replace ("\"", "");
                string[][] commands = new [] {
                    new [] { "commit", "-a", "-m", msg },
                    new [] { "pull" },
                    new [] { "push" },
                };
                int i = 1;
                foreach (string path in RepoPaths) {
                    Log.Message ("Commit: ", path);
                    GitLibrary.Execute (commands: commands, verbose: true, rootDirectory: path);
                    ++i;
                }
            } else {
                Log.Message ("No repositories.");
            }
        }

        private void addRepo (string path)
        {
            RepoPaths.Add (path);
            SaveRepoPaths ();
        }

        private void removeRepo (string path)
        {
            RepoPaths.Remove (path);
            SaveRepoPaths ();
        }

        private void list ()
        {
            if (RepoPaths.Count >= 1) {
                Log.Message ("List of repositories:");
                int i = 1;
                foreach (string path in RepoPaths) {
                    Log.Message ("  ", i, ". ", path);
                    ++i;
                }
            } else {
                Log.Message ("No repositories.");
            }
        }

        private void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}


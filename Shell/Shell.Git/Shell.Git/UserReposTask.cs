using System;
using System.Linq;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.IO;
using System.Collections.Generic;
using Mono.Options;

namespace Shell.Git
{
    public class UserReposTask : MonoOptionsScriptTask, MainScriptTask
    {
        private ConfigFile config;

        public UserReposTask ()
        {
            Name = "UserRepos";
            Options = new [] { "user-repos" };
            ConfigName = "UserRepos";

            Description = new [] {
                "Commit changes in the user's selected repositories",
                "Push the user's selected repositories",
                "Show the status of the user's selected repositories",
                "Add a repository to the list",
                "List the repositories",
                "Remove an repository from the list"
            };
            Parameters = new [] {
                "commit",
                "push",
                "status",
                "add",
                "list",
                "remove"
            };
            Methods = new Action[] {
                () => commit (),
                () => push (),
                () => status (),
                () => add (),
                () => list (),
                () => remove (),
            };
        }

        protected override void HookBeforeOptionParsing ()
        {
            base.HookBeforeOptionParsing ();

            Log.DEBUG_ENABLED = true;

            config = fs.Config.OpenConfigFile ("userrepos.ini");
        }

        private string paramPath = null;

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet.Add ("path=", "The path to add or remove.", option => paramPath = option);
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
            RepoPaths = new HashSet<string> (from path in RepoPaths
                                                      select path.TrimEnd (new char[] { '/', '\\' }));
            config ["UserRepos", "paths", ""] = string.Join (":", RepoPaths);
        }


        private void commit ()
        {
            fs.Runtime.RequirePackages ("git");

            if (RepoPaths.Count >= 1) {
                string msg = "committed by: " + string.Join (" ", Environment.GetCommandLineArgs ()).Replace ("\"", "");
                string[][] commands = new [] {
                    new [] { "commit", "-a", "-m", msg },
                    new [] { "pull" },
                    new [] { "push" },
                };
                int i = 1;
                foreach (string path in RepoPaths) {
                    Log.Info ("Commit: ", path);
                    GitLibrary.Execute (commands: commands, verbose: true, rootDirectory: path);
                    ++i;
                }
            } else {
                Log.Info ("No repositories.");
            }
        }


        private void push ()
        {
            fs.Runtime.RequirePackages ("git");

            if (RepoPaths.Count >= 1) {
                string[][] commands = new [] {
                    new [] { "pull" },
                    new [] { "push" },
                };
                int i = 1;
                foreach (string path in RepoPaths) {
                    Log.Info ("Push: ", path);
                    GitLibrary.Execute (commands: commands, verbose: true, rootDirectory: path);
                    ++i;
                }
            } else {
                Log.Info ("No repositories.");
            }
        }

        private void status ()
        {
            fs.Runtime.RequirePackages ("git");

            if (RepoPaths.Count >= 1) {
                string[][] commands = new [] {
                    new [] { "status" },
                };
                int i = 1;
                foreach (string path in RepoPaths) {
                    Log.Info ("Status: ", path);
                    GitLibrary.Execute (commands: commands, verbose: true, rootDirectory: path);
                    ++i;
                }
            } else {
                Log.Info ("No repositories.");
            }
        }

        private void add ()
        {
            if (paramPath == null) {
                Log.Error ("Invalid parameters. The option '--path' has to be specified.");
                return;
            }

            addRepo (paramPath);
            Log.Info ("Added repository: ", paramPath);
            list ();
        }

        private void remove ()
        {
            if (paramPath == null) {
                Log.Error ("Invalid parameters. The option '--path' has to be specified.");
                return;
            }

            removeRepo (paramPath);
            Log.Info ("Removed repository: ", paramPath);
            list ();
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
                Log.Info ("List of repositories:");
                int i = 1;
                foreach (string path in RepoPaths) {
                    Log.Info ("  ", i, ". ", path);
                    ++i;
                }
            } else {
                Log.Info ("No repositories.");
            }
        }
    }
}


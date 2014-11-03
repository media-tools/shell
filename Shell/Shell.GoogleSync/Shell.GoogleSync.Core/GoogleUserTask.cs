using System;
using Shell.Common.Tasks;
using Shell.Common.IO;
using System.Collections.Generic;
using System.Linq;

namespace Shell.GoogleSync.Core
{
    public class GoogleUserTask : ScriptTask, MainScriptTask
    {
        public GoogleUserTask ()
        {
            Name = "GoogleUser";
            Description = new [] {
                "Add a google account",
                "List all google accounts",
                "Try to authenticate all google acounts"
            };
            Options = new [] { "google-users", "g-users" };
            ConfigName = "Google";
            ParameterSyntax = new [] { "add", "list", "auth" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "add":
                    addAccount ();
                    list ();
                    break;
                case "list":
                    list ();
                    break;
                case "auth":
                    auth ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void addAccount ()
        {
            
            GoogleApp appConfig = new GoogleApp ();
            appConfig.Authenticate ();
        }

        void list ()
        {
            IEnumerable<GoogleAccount> accounts = GoogleAccount.List ();
            if (accounts.Any ()) {
                Log.Message (accounts.ToStringTable (
                    new[] { "Name", "E-Mail Address", "ID" },
                    acc => acc.DisplayName,
                    acc => acc.Emails,
                    acc => acc.Id
                ));
            } else {
                Log.Message ("There are no accounts.");
            }
        }

        void auth ()
        {
            GoogleApp appConfig = new GoogleApp ();
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent++;
                if (appConfig.Authenticate (acc)) {
                    Log.Message ("Success!");
                } else {
                    Log.Message ("Failure!");
                }
                Log.Indent--;
            }
        }

        void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}


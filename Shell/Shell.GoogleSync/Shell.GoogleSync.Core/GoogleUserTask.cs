using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Options;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Namespaces;

namespace Shell.GoogleSync.Core
{
    public class GoogleUserTask : MonoOptionsScriptTask, MainScriptTask
    {
        public GoogleUserTask ()
        {
            Name = "GoogleUser";
            Options = new [] { "google-users" };
            ConfigName = NamespaceGoogle.CONFIG_NAME;

            Description = new [] {
                "Add a google account",
                "List all google accounts",
                "Try to authenticate all google acounts"
            };
            Parameters = new [] {
                "add",
                "list",
                "auth"
            };
            Methods = new Action[] {
                () => {
                    addAccount ();
                    list ();
                },
                () => list (),
                () => auth (),
            };
        }

        protected override void SetupOptions (ref OptionSet optionSet)
        {
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
    }
}


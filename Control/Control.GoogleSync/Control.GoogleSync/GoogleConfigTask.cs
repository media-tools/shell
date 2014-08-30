using System;
using Control.Common.Tasks;
using Control.Common.IO;

namespace Control.GoogleSync
{
    public class GoogleConfigTask : Task, MainTask
    {
        public GoogleConfigTask ()
        {
            Name = "GoogleConfig";
            Description = "Edit Google App settings";
            Options = new string[] { "google-config", "g-config" };
            ConfigName = "Google";
            ParameterSyntax = "add | list | auth";
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
            GoogleAppConfig appConfig = new GoogleAppConfig ();
            appConfig.Authenticate ();
        }

        void list ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
            }
        }

        void auth ()
        {
            GoogleAppConfig appConfig = new GoogleAppConfig ();
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent ++;
                if (appConfig.Authenticate (acc)) {
                    Log.Message ("Success!");
                } else {
                    Log.Message ("Failure!");
                }
                Log.Indent --;
            }
        }

        void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}


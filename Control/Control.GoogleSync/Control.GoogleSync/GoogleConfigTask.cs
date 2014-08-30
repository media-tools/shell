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
        }

        protected override void InternalRun (string[] args)
        {
            GoogleAppConfig appConfig = new GoogleAppConfig ();
            appConfig.Authenticate ();
        }
    }
}


using System;
using Control.Common.Tasks;

namespace Control.GoogleSync
{
    public class GoogleContactsSyncTask : Task, MainTask
    {
        public GoogleContactsSyncTask ()
        {
            Name = "GoogleContacts";
            Description = "fuck";
            Options = new string[] { "google-contacts", "g-contacts" };
            ConfigName = "Google";
        }

        protected override void InternalRun (string[] args)
        {
        }
    }
}

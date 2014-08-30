using System;
using Control.Common;
using Control.Common.IO;
using Google.Contacts;
using Google.GData.Client;
using System.Linq;

namespace Control.GoogleSync
{
    public class GoogleAccount : Library
    {
        public GoogleAccount (string name, OAuth2Parameters parameters)
        {
            ConfigName = "Google";
            ConfigFile accountConfig = fs.Config.OpenConfigFile ("accounts.ini");

            accountConfig ["General", "account_list", ""] = string.Join (";", accountConfig ["General", "account_list", ""].Split (new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Concat (new string[] { }));
            accountConfig ["Acc_"+name, "AccessToken", ""] = parameters.AccessToken;
            accountConfig ["Acc_"+name, "RefreshToken", ""] = parameters.RefreshToken;
            accountConfig ["Acc_"+name, "TokenExpiry", ""] = parameters.TokenExpiry.ToLongDateString();

            try {
                RequestSettings settings = new RequestSettings(GoogleAppConfig.ApplicationName, parameters);
                ContactsRequest cr = new ContactsRequest(settings);

                Feed<Contact> f = cr.GetContacts();
                foreach (Contact c in f.Entries) {
                    Console.WriteLine(c.Name.FullName);
                }
            } catch (Exception ex) {
                Log.Error (ex);
            }
        }
	}
}


using System;
using System.Collections.Generic;
using System.Linq;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.Contacts;
using Control.Common.IO;

namespace Control.GoogleSync
{
    public static class GDataExtensions
    {
        public static OAuth2Parameters GetOAuth2Parameters (this GoogleAccount account)
        {
            GoogleApp appConfig = new GoogleApp ();

            OAuth2Parameters parameters = new OAuth2Parameters {
                ClientId = appConfig.ClientId,
                ClientSecret = appConfig.ClientSecret,
                RedirectUri = GoogleApp.RedirectUri,
                Scope = string.Join (" ", GoogleApp.Scopes),
            };
            parameters.AccessToken = account.AccessToken;
            parameters.RefreshToken = account.RefreshToken;

            return parameters;
        }

        public static string Join (this IEnumerable<EMail> mails, string separator)
        {
            return string.Join (separator, from mail in mails
                                                    select mail.Address);
        }

        public static string PrintShort (this IEnumerable<EMail> mails)
        {
            string[] addrs = (from mail in mails
                                       select mail.Address).ToArray ();
            if (addrs.Length == 1) {
                return addrs [0];
            } else if (addrs.Length > 1) {
                return mails.PrimaryAddress () + ";...";
            } else {
                return "";
            }
        }

        public static string PrimaryAddress (this IEnumerable<EMail> mails)
        {
            IEnumerable<EMail> gmail = from mail in mails
                                                where mail.Address.Contains ("gmail.com")
                                                select mail;
            if (gmail.Any ()) {
                return gmail.First ().Address;
            } else if (mails.Any ()) {
                return mails.First ().Address;
            } else {
                return "";
            }
        }

        public static string PrimaryNumber (this IEnumerable<PhoneNumber> numbers)
        {
            if (numbers.Any ()) {
                return numbers.First ().ToString ();
            } else {
                return "";
            }
        }

        /*
        public static string Print (this Contact contact)
        {
            string email = contact.Emails.PrimaryAddress ();
            return contact.Name.FullName + (email.Length == 0 ? " <?>" : " <" + email + ">");
        }*/

        public static bool IsIncludedInSynchronisation (this Contact contact)
        {
            return (from name in Contacts.IncludeNames
                             where contact.Name.FullName != null && contact.Name.FullName.ToLower ().Contains (name.ToLower ())
                             select name).Any ();
        }

        public static bool IsMasterAccount (this GoogleAccount account)
        {
            return (from id in Contacts.MasterAccountIds
                             where account.Id == id
                             select id).Any ();
        }

        public static bool IsSlaveAccount (this GoogleAccount account)
        {
            return !account.IsMasterAccount ();
        }

        public static void Merge <A> (ExtensionCollection<A> to, ExtensionCollection<A> from, Func<A, string> comparable, Func<A, A> format = null) where A : class, IExtensionElementFactory, new()
        {
            format = format ?? (a => a);
            ExtensionCollection<A> merged = new ExtensionCollection<A> ();
            HashSet<string> uniqueStrings = new HashSet<string> ();
            foreach (A _obj in from.Concat(to)) {
                A obj = format (_obj);
                string comp = comparable (obj);
                if (!uniqueStrings.Contains (comp)) {
                    merged.Add (obj);
                    uniqueStrings.Add (comp);
                }
            }

            to.Clear ();
            foreach (A obj in merged) {
                to.Add (obj);
            }
        }

        public static PhoneNumber UniqueFormat (this PhoneNumber number)
        {
            string value = number.Value;
            value = value.Replace ("-", "").Replace (" ", "");
            if (value.StartsWith ("0")) {
                value = "+49" + value.Substring (1);
            }
            if (number.Rel != null) {
                return new PhoneNumber () { Value = value, Rel = number.Rel };
            } else {
                return new PhoneNumber () { Value = value, Label = number.Label };
            }
        }

        public static string Format (this StructuredPostalAddress address)
        {
            if (address.Street == null || address.City == null || address.Postcode == null) {
                return address.FormattedAddress.Replace ("\n", ", ") + " (unstructured)";
            } else {
                return string.Format ("{0}, {1} {2}", address.Street.Trim (), address.Postcode.Trim (), address.City.Trim ()).Replace ("\n", ", ");
            }
        }
    }
}


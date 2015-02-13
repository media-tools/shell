using System;
using Shell.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MailKit.Net.Imap;
using Shell.Common.Util;

namespace Shell.MailSync
{
    public class Account : ValueObject<Account>
    {
        public string Accountname { get; private set; }

        public string Hostname { get; private set; }

        public string Username { get; private set; }

        public string Password { get; private set; }

        public Account (string accountname, string hostname, string username, string password)
        {
            Accountname = filterAccountname.Replace (accountname.ToLower (), "");
            Hostname = hostname;
            Username = username;
            Password = password;
        }

        public bool IsDirectDeleteEnabled {
            get {
                return Hostname == "imap.de.aol.com";
            }
        }

        public bool HasName (string name)
        {
            return Accountname == filterAccountname.Replace (name.ToLower (), "");
        }

        public override string ToString ()
        {
            return "Account(name=" + Accountname + ",host=" + Hostname + ",user=" + Username + ")"; //+ ",pass=" + String.Concat (Enumerable.Repeat ("?", Password.Length)) + ")";
        }

        private static Regex filterAccountname = new Regex ("[^a-zA-Z0-9]");

        public void ConnectAndAuthenticate (ImapClient client)
        {
            client.Connect (hostName: Hostname, port: 993, useSsl: true);
            client.AuthenticationMechanisms.Remove ("XOAUTH");
            client.Authenticate (userName: Username, password: Password);
        }

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Accountname, Hostname, Username };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<Account>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (Account a, Account b)
        {
            return ValueObject<Account>.Equality (a, b);
        }

        public static bool operator != (Account a, Account b)
        {
            return ValueObject<Account>.Inequality (a, b);
        }
    }
}

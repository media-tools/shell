using System;
using System.Collections.Generic;
using MailKit;
using MailKit.Net.Imap;
using Shell.Common.IO;
using System.Linq;
using System.IO;

namespace Shell.MailSync
{
	public class ImapClientWrapper
	{
		// global constants
		public static readonly int MAX_REPEATS = 3;

		// log
		public TaggedLog taggedLog;

		// clients cache
		private static Dictionary<string, ImapClient> clientsCache = new Dictionary<string, ImapClient> ();
		private static Dictionary<string, int> reconnectCounterCache = new Dictionary<string, int> ();

		// reconnect counter
		public int ReconnectCounter {
			get { return reconnectCounterCache.ContainsKey (key) ? reconnectCounterCache [key] : reconnectCounterCache [key] = 0; }
			set { reconnectCounterCache [key] = value; }
		}

		// this instance
		private Account account;
		private string token;
		private string key;

		private ImapClient client { get { return GetClient (); } }

		// current folder
		public string CurrentFolder { get; set; }

		public ImapClientWrapper (Account account, string token)
		{
			this.account = account;
			this.token = token;
			this.key = account.Accountname + token;
			taggedLog = Log.TaggedNamespace (logNamespace: "mailsync");
		}

		private ImapClient GetClient ()
		{
			ImapClient client = null;
			if (clientsCache.ContainsKey (key)) {
				client = clientsCache [key];
				if (!client.IsConnected) {
					taggedLog.Message ("imap disconnected", account.Accountname, ", reconnect counter: ", ReconnectCounter);
					client.Dispose ();
					client = null;
				}
			}
			if (client == null) {
				taggedLog.Message ("imap connect", account.Accountname, ", reconnect counter: ", ReconnectCounter);
				//client = new ImapClient (new ProtocolLogger ("mailkit_" + account.Accountname + "_" + token + ".log"));
				ReconnectCounter++;
				client = new ImapClient ();
				account.ConnectAndAuthenticate (client: client);
				clientsCache [key] = client;
			} else {
				//taggedLog.Message ("imap cached", account.Accountname, ", reconnect counter: ", ReconnectCounter);
			}
			return client;
		}

		public void Reconnect ()
		{
			taggedLog.Message ("imap reconnect", account.Accountname);
			clientsCache.Remove (key);
			GetClient ();
			if (!string.IsNullOrWhiteSpace (CurrentFolder)) {
				taggedLog.Message ("re-open folder", CurrentFolder);
				client.GetFolder (CurrentFolder).Open (FolderAccess.ReadWrite);
			}
		}

		public ImapFolderWrapper GetFolderOrCreate (string path)
		{
			ImapFolderWrapper result = null;
			Try (() => {
				SpecialFolder[] specialFolders = new SpecialFolder[] {
					SpecialFolder.Trash,
					SpecialFolder.Sent,
					SpecialFolder.Junk,
					SpecialFolder.Drafts,
					SpecialFolder.Archive
				};
				foreach (SpecialFolder specialFolder in specialFolders) {
					if (path == specialFolder + "") {
						result = GetFolder (specialFolder);
					}
				}

				if (result == null) {
					result = GetFolder (path);
				}
				if (result == null) {
					Log.Debug ("namespaces: ", string.Join (",", PersonalNamespaces.Select (ns => ns.Path)));
					GetFolder (PersonalNamespaces [0]).Create (name: path, isMessageFolder: true);
					result = GetFolder (path);
				}
				if (result == null) {
					throw new ArgumentException ("Can't create folder: ", path);
				}
			});
			return result;
		}

		public ImapClientResult Try (Action action)
		{
			ImapClientResult result = ImapClientResult.None;
			Exception relayedException = null;
			int maxRepeats = MAX_REPEATS;
			do {
				try {
					action ();
					result = ImapClientResult.Success;
				} catch (Exception ex) {
					if (ex is ImapProtocolException || ex is IOException || ex is InvalidOperationException) {
						Log.Error ("Imap client died after fatal exception!");
						Log.Error (ex);
						if (--maxRepeats > 0) {
							Log.Error ("Trying to reconnect...");
							Reconnect ();
							result = ImapClientResult.Repeat;
						} else {
							Log.Error ("Too many reconnects...");
							result = ImapClientResult.Error;
							relayedException = new TriedTooOftenException (ex);
						}
					} else {
						Log.Error ("Exception that doesn't trigger a reconnect:");
						Log.Error (ex);
						result = ImapClientResult.Error;
						relayedException = new RelayException (exception: ex);
					}
				}
			} while (result == ImapClientResult.Repeat);
			if (relayedException != null) {
				throw relayedException;
			}
			return result;
		}

		// deletegate everything else to MailKit.ImapClient

		public FolderNamespaceCollection PersonalNamespaces { get { return client.PersonalNamespaces; } }

		public ImapFolderWrapper GetFolder (SpecialFolder folder)
		{
			return new ImapFolderWrapper (client: this, fullName: client.GetFolder (folder).FullName);
		}

		public ImapFolderWrapper GetFolder (FolderNamespace folder)
		{
			return new ImapFolderWrapper (client: this, fullName: client.GetFolder (folder).FullName);
		}

		public ImapFolderWrapper GetFolder (string folder)
		{
			Log.Debug ("client: ", this);
			Log.Debug ("folder: ", folder);
			Log.Debug ("client.GetFolder (folder): ", client.GetFolder (folder));
			return new ImapFolderWrapper (client: this, fullName: client.GetFolder (folder).FullName);
		}

		public IMailFolder GetInternalFolder (string folder)
		{
			return client.GetFolder (folder);
		}
	}

	public enum ImapClientResult
	{
		None = 0,
		Success,
		Repeat,
		Error
	}
}


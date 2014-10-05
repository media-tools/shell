using System;
using MailKit.Net.Imap;
using MailKit;
using Shell.Common.IO;
using System.Collections.Generic;
using MailKit.Search;
using System.Linq;
using Shell.Common.Util;

namespace Shell.MailSync
{
	public class MailSyncLibrary
	{
		private MailLibrary lib;

		public MailSyncLibrary (MailLibrary mailLibrary)
		{
			lib = mailLibrary;
		}

		public void ListAccounts ()
		{
			ProgressBar progressBar = Log.OpenProgressBar (identifier: "MailSyncLibrary:ListAccounts", description: "Checking accounts...");
			Dictionary<Account, int> inboxMessages = new Dictionary<Account, int> ();
			foreach (Account account in lib.Accounts) {
				using (var client = new ImapClient ()) {
					progressBar.Next (printAlways: true, currentDescription: account.Username);

					account.ConnectAndAuthenticate (client: client);
					var inbox = client.Inbox;
					inbox.Open (FolderAccess.ReadOnly);
					inboxMessages [account] = inbox.Count;
				}
			}
			progressBar.Finish ();

			Log.Message (lib.Accounts.ToStringTable (
				acc => inboxMessages.ContainsKey (acc) ? LogColor.Reset : LogColor.DarkCyan,
				new[] { "User Name", "Server", "Inbox Messages" },
				acc => acc.Username,
				acc => acc.Hostname,
				acc => inboxMessages.ContainsKey (acc) ? inboxMessages [acc] + "" : "-"
			));
		}

		private void analyzeFolder (IMailFolder above, ref ProgressBar progressBar, ref Dictionary<string, int> folderMessages)
		{
			foreach (IMailFolder folder in above.GetSubfolders (false)) {
				try {
					progressBar.Next (printAlways: true, currentDescription: folder.FullName);
					folder.Open (FolderAccess.ReadOnly);
					folderMessages [folder.FullName] = folder.Count;
				} catch (ImapCommandException) {
				}
				analyzeFolder (folder, ref progressBar, ref folderMessages);
			}
		}

		public void ListFolders ()
		{
			foreach (Account account in lib.Accounts) {
				Log.Message ("Folders in ", account);

				ProgressBar progressBar = Log.OpenProgressBar (identifier: "MailSyncLibrary:ListFolders:" + account.Accountname, description: "Checking folders...");
				Dictionary<string, int> folderMessages = new Dictionary<string, int> ();
				using (var client = new ImapClient ()) {
					account.ConnectAndAuthenticate (client: client);
					foreach (FolderNamespace ns in client.PersonalNamespaces) {
						var personal = client.GetFolder (ns);
						analyzeFolder (personal, ref progressBar, ref folderMessages);
					}
				}
				progressBar.Finish ();

				Log.Message (folderMessages.Keys.ToStringTable (
					fold => LogColor.Reset,
					new[] { "Folder", "Inbox Messages" },
					fold => fold,
					fold => folderMessages [fold]
				));
			}
		}

		private void Copy (IMailFolder fromFolder, IMailFolder toFolder, MessageList notThere)
		{
			try {
				foreach (IMessageSummary summary in notThere) {
					// Console.WriteLine ("[copy] {0:D2}: {1} {2} {3}", summary.Index, summary.Envelope.Subject, summary.SortableDate, summary.Envelope.MessageId);
					Console.WriteLine ("[copy] {0:D2}: {1} {2} {3} (key: {4})", summary.Index, summary.Envelope.Subject, summary.SortableDate, summary.Envelope.MessageId, summary);

					MimeKit.MimeMessage message = fromFolder.GetMessage (uid: summary.UniqueId.Value);
					toFolder.Append (message: message, flags: summary.Flags.HasValue ? summary.Flags.Value : 0);
				}
			} catch (Exception ex) {
				Log.Error (ex);
			}
		}

		public void Sync ()
		{
			foreach (Channel channel in lib.Channels) {
				using (var fromClient = new ImapClient ()) {
					using (var toClient = new ImapClient ()) {
						try {
							channel.FromAccount.ConnectAndAuthenticate (client: fromClient);
							channel.ToAccount.ConnectAndAuthenticate (client: toClient);

							IMailFolder fromFolder = fromClient.GetFolderOrCreate (path: channel.FromPath);
							fromFolder.Open (FolderAccess.ReadOnly);
							int fromCount = fromFolder.Count;
							MessageList fromList = fromFolder.GetMessages ();

							IMailFolder toFolder = toClient.GetFolderOrCreate (path: channel.ToPath);
							toFolder.Open (FolderAccess.ReadOnly);
							int toCount = toFolder.Count;
							MessageList toList = toFolder.GetMessages ();

							MessageList alreadyThere = fromList.Intersect (toList);
							MessageList notThere = fromList.Except (toList);
							//Log.Debug (string.Join (", ", alreadyThere.Select (sum => sum.Envelope.MessageId)));
							Log.Message ("Sync: ", channel.FromFullName, " -> ", channel.ToFullName,
								" | source: ", fromCount, ", target: ", toCount,
								", alreadyThere: ", alreadyThere.Count, ", notThere: ", notThere.Count);

							Copy (fromFolder, toFolder, notThere);
						} catch (Exception ex) {
							Log.Error (ex);
						}
					}
				}
			}
		}
	}
}


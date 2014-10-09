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

		private void DeleteMessage (IMailFolder fromFolder, IMessageSummary summary)
		{
			Log.Message (string.Format ("[delete] {0}", summary.Print ()));

		}

		private void CopyMessage (IMailFolder fromFolder, IMailFolder toFolder, IMessageSummary summary)
		{
			Log.Message (string.Format ("[copy]   {0}", summary.Print ()));

			MimeKit.MimeMessage message = fromFolder.GetMessage (uid: summary.UniqueId.Value);
			Nullable<UniqueId> uid = toFolder.Append (message: message, flags: summary.Flags.HasValue ? summary.Flags.Value : MessageFlags.None, date: summary.InternalDate.Value);

			// success?
			if (uid.HasValue) { 
				MimeKit.MimeMessage copiedMessage = toFolder.GetMessage (uid.Value);
				IList<IMessageSummary> copiedSummarys = toFolder.Fetch (new UniqueId[] { uid.Value }.ToList (), MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
				if (copiedSummarys.Any ()) {
					string origKey = MessageList.KEY (summary);
					string copyKey = MessageList.KEY (copiedSummarys.First ());
					DateTimeOffset origDate = message.Date;
					DateTimeOffset copyDate = copiedMessage.Date;
					string origId = summary.Envelope.MessageId;
					string copyId = copiedSummarys.First ().Envelope.MessageId;
					if (origKey != copyKey || origDate != copyDate || origId != copyId) {
						Log.Debug (string.Format ("    original key: {0}", origKey));
						Log.Debug (string.Format ("    copied   key: {0}", copyKey));
						Log.Debug (string.Format ("    original message date: {0}", origDate));
						Log.Debug (string.Format ("    copied   message date: {0}", copyDate));
						Log.Debug (string.Format ("    original message id: {0}", origId));
						Log.Debug (string.Format ("    copied   message id: {0}", copyId));
					}
				} else {
					Log.Debug ("    no IMessageSummary's for that uid: ", uid.Value);
				}
			} else {
				Log.Debug ("    no uid for that copy.");
			}
		}

		private bool ApplyFilter (IMessageSummary summary, Dictionary<string,string> parameters)
		{
			if (parameters.ContainsKey ("filter")) {
				string[] all = new string[] {
					summary.Envelope.Subject,
					summary.Envelope.From == null ? string.Empty : string.Join (" ", summary.Envelope.From.Where (i => i != null && i.Name != null).Select (i => i.Name)),
					summary.Envelope.To == null ? string.Empty : string.Join (" ", summary.Envelope.To.Where (i => i != null && i.Name != null).Select (i => i.Name)),
					summary.SortableFrom,
					summary.SortableTo,
					
					summary.SortableCc
				}.Where (x => !string.IsNullOrWhiteSpace (x)).Select (x => x.ToLower ().Trim ()).ToArray ();

				string[] includedStrings = parameters ["filter"]
					.Split (new string[]{ "," }, StringSplitOptions.RemoveEmptyEntries)
					.Select (x => x.ToLower ().Trim ()).ToArray ();

				foreach (string includedString in includedStrings) {
					foreach (string element in all) {
						if (element.Contains (includedString)) {
							Log.Message (string.Format ("[filter] [included] {0}", summary.Print ()));
							return true;
						}
					}
				}

				//Log.Message (string.Format ("[filter] [not included] {0}", summary.Print ()));
				return false;
			} else {
				//Log.Message (string.Format ("[filter] [dont care]"));
				return true;
			}
		}

		private MessageList ApplyFilter (MessageList unfiltered, Dictionary<string,string> parameters)
		{
			MessageList result = new MessageList ();
			foreach (IMessageSummary summary in unfiltered) {
				if (ApplyFilter (summary: summary, parameters: parameters)) {
					result.Add (summary);
				}
			}
			return result;
		}

		private void CopyFolder (IMailFolder fromFolder, IMailFolder toFolder, MessageList messages, Dictionary<string,string> parameters)
		{
			foreach (IMessageSummary summary in messages) {
				try {
					CopyMessage (fromFolder: fromFolder, toFolder: toFolder, summary: summary);
				} catch (Exception ex) {
					Log.Error (ex);
					if (ex is System.IO.IOException) {
						Log.Error ("Copying stopped after fatal exception!");
						break;
					}
				}
			}
		}

		private void MoveFolder (IMailFolder fromFolder, IMailFolder toFolder, MessageList messages, Dictionary<string,string> parameters)
		{
			foreach (IMessageSummary summary in messages) {
				try {
					CopyMessage (fromFolder: fromFolder, toFolder: toFolder, summary: summary);
					DeleteMessage (fromFolder: fromFolder, summary: summary);
				} catch (Exception ex) {
					Log.Error (ex);
					if (ex is System.IO.IOException) {
						Log.Error ("Moving stopped after fatal exception!");
						break;
					}
				}
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

							IMailFolder[] fromFolders;
							if (channel.FromPath == "*") {
								fromFolders = fromClient.GetAllFolders ().ToArray ();
								Log.Debug ("wildcard expands to: ", string.Join (", ", fromFolders.Select (f => f.FullName)));
							} else {
								fromFolders = new IMailFolder[] { fromClient.GetFolderOrCreate (path: channel.FromPath) };
							}

							foreach (IMailFolder fromFolder in fromFolders) {
								IMailFolder toFolder = toClient.GetFolderOrCreate (path: channel.ToPath);

								Log.Message ("[sync folder] ", channel.FromAccount.Accountname, ":", fromFolder.FullName,
									" -> ", channel.ToAccount.Accountname, ":", toFolder.FullName);

								fromFolder.Open (FolderAccess.ReadWrite);
								int fromCount = fromFolder.Count;
								MessageList fromList = fromFolder.GetMessages ();

								toFolder.Open (FolderAccess.ReadWrite);
								int toCount = toFolder.Count;
								MessageList toList = toFolder.GetMessages ();

								MessageList alreadyThere = fromList.Intersect (toList);
								MessageList notThere = fromList.Except (toList);
								MessageList needToCopy = ApplyFilter (unfiltered: notThere, parameters: channel.Parameters);

								Log.Message ("              ", string.Join (", ", new string[] {
									"source: " + fromCount, "target: " + toCount,
									"already there: " + alreadyThere.Count, "not there: " + notThere.Count,
									"need to copy: " + needToCopy.Count, "filtered: " + (notThere.Count - needToCopy.Count)
								}));

								if (channel.Operation == ChannelOperation.COPY) {
									CopyFolder (fromFolder, toFolder, needToCopy, channel.Parameters);
								} else if (channel.Operation == ChannelOperation.MOVE) {
									MoveFolder (fromFolder, toFolder, needToCopy, channel.Parameters);
								}
								

								toFolder.Open (FolderAccess.ReadOnly);
								MessageList toListLater = toFolder.GetMessages ();
								MessageList alreadyThereLater = fromList.Intersect (toListLater);
								MessageList notThereLater = fromList.Except (toListLater);
								MessageList needToCopyLater = ApplyFilter (unfiltered: notThereLater, parameters: channel.Parameters);
								if (needToCopyLater.Count > 0) {
									if (channel.Operation == ChannelOperation.COPY) {
										Log.Message ("Sync failed: copied: ", (needToCopy.Count - needToCopyLater.Count), ", failed to copy: ", needToCopyLater.Count);
									} else if (channel.Operation == ChannelOperation.MOVE) {
										Log.Message ("Sync failed: moved: ", (needToCopy.Count - needToCopyLater.Count), ", failed to move: ", needToCopyLater.Count);
									}
								}
							}

						} catch (Exception ex) {
							Log.Error (ex);
						}
					}
				}
			}
		}
	}
}


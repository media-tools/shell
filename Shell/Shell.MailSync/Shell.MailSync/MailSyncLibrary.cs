using System;
using MailKit.Net.Imap;
using MailKit;
using Shell.Common.IO;
using System.Collections.Generic;
using MailKit.Search;
using System.Linq;
using Shell.Common.Util;
using System.Threading.Tasks;

namespace Shell.MailSync
{
	public class MailSyncLibrary
	{
		private MailLibrary lib;
		public TaggedLog taggedLog;

		public MailSyncLibrary (MailLibrary mailLibrary)
		{
			lib = mailLibrary;

			Log.SetupTaggedLog (logNamespace: "mailsync", maxWidth: 20);
			taggedLog = Log.TaggedNamespace (logNamespace: "mailsync");
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

		private async Task DeleteMessages (IMailFolder folder, IEnumerable<IMessageSummary> block)
		{
			taggedLog.Message ("delete block", block.First ().Print ());
			foreach (IMessageSummary other in block.Skip(1)) {
				taggedLog.Message ("", other.Print ());
			}
			await folder.AddFlagsAsync (block.Select (summary => summary.UniqueId.Value).ToList (), MessageFlags.Deleted, true);
		}

		private void CopyMessage (IMailFolder fromFolder, IMailFolder toFolder, IMessageSummary summary, bool verify)
		{
			Task task = new Task (async () => {
				await CopyMessages (fromFolder: fromFolder, toFolder: toFolder, block: new IMessageSummary[]{ summary }, verify: verify);
			});
			task.Start ();
			task.Wait ();
		}

		struct CopiedMessage
		{
			public IMessageSummary SourceSummary;
			public MimeKit.MimeMessage SourceMessage;
			public UniqueId? TargetUid;
			
		};

		private async Task verifyMessage (IMailFolder toFolder, List<CopiedMessage> copiedMessages)
		{
			foreach (CopiedMessage copiedMessageInfo in copiedMessages) {
				MimeKit.MimeMessage message = copiedMessageInfo.SourceMessage;
				IMessageSummary summary = copiedMessageInfo.SourceSummary;
				UniqueId? uid = copiedMessageInfo.TargetUid;

				// success?
				if (uid.HasValue) {
					MimeKit.MimeMessage copiedMessage = await toFolder.GetMessageAsync (uid.Value);
					IList<IMessageSummary> copiedSummarys = await toFolder.FetchAsync (new UniqueId[] { uid.Value }.ToList (), MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
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
		}

		private async Task CopyMessages (IMailFolder fromFolder, IMailFolder toFolder, IEnumerable<IMessageSummary> block, bool verify)
		{
			if (block.Count () == 0) {
				Log.Error ("CopyMessage: block is empty");
				return;
			}

			if (block.Count () == 1) {
				taggedLog.Message ("copy", block.First ().Print ());
			} else {
				taggedLog.Message ("copy block", block.First ().Print ());
				foreach (IMessageSummary other in block.Skip(1)) {
					taggedLog.Message ("", other.Print ());
				}
			}

			List<CopiedMessage> copiedMessages = new List<CopiedMessage> ();

			foreach (IMessageSummary summary in block) {
				MimeKit.MimeMessage message = await fromFolder.GetMessageAsync (uid: summary.UniqueId.Value);
				Nullable<UniqueId> uid = await toFolder.AppendAsync (message: message, flags: summary.Flags.HasValue ? summary.Flags.Value : MessageFlags.None, date: summary.InternalDate.Value);
				if (verify) {
					copiedMessages.Add (new CopiedMessage {
						SourceMessage = message,
						SourceSummary = summary,
						TargetUid = uid
					});
				}
			}

			if (verify) {
				await verifyMessage (toFolder: toFolder, copiedMessages: copiedMessages);
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
					.Split (new string[]{ ",", "|" }, StringSplitOptions.RemoveEmptyEntries)
					.Select (x => x.ToLower ().Trim ()).ToArray ();

				foreach (string includedString in includedStrings) {
					foreach (string element in all) {
						if (element.Contains (includedString)) {
							taggedLog.Message ("filter", string.Format ("[included] {0}", summary.Print ()));
							return true;
						}
					}
				}

				//taggedLog.Message ("filter", string.Format ("[not included] {0}", summary.Print ()));
				return false;
			} else {
				//taggedLog.Message ("filter", string.Format ("[dont care]"));
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
			int blocksize = 10;
			int i = 0;
			int count = messages.Count;
			while (i < count) {
				IEnumerable<IMessageSummary> block = messages.Skip (i).Take (blocksize);
				i += blocksize;
				try {
					Task.Run (async () => {
						await CopyMessages (fromFolder: fromFolder, toFolder: toFolder, block: block, verify: true);
					}).Wait ();
				} catch (Exception ex) {
					Log.Error (ex);
					if (ex is System.IO.IOException || ex is InvalidOperationException) {
						Log.Error ("Copying stopped after fatal exception!");
						break;
					}
				}
			}
		}

		private void MoveFolder (IMailFolder fromFolder, IMailFolder toFolder, MessageList messages, Dictionary<string,string> parameters)
		{
			int blocksize = 10;
			int i = 0;
			int count = messages.Count;
			while (i < count) {
				IEnumerable<IMessageSummary> block = messages.Skip (i).Take (blocksize);
				i += blocksize;
				try {

					Task.Run (async () => {
						await CopyMessages (fromFolder: fromFolder, toFolder: toFolder, block: block, verify: false);
						await DeleteMessages (folder: fromFolder, block: block);
					}).Wait ();
				} catch (Exception ex) {
					Log.Error (ex);
					if (ex is System.IO.IOException || ex is InvalidOperationException) {
						Log.Error ("Moving stopped after fatal exception!");
						break;
					}
				}
			}
		}

		private ImapClient GetClient (Account account, ref Dictionary<string, ImapClient> clientsCache, string token)
		{
			string key = account.Accountname + token;
			ImapClient client = null;
			if (clientsCache.ContainsKey (key)) {
				client = clientsCache [key];
				if (!client.IsConnected) {
					taggedLog.Message ("imap disconnected", account.Accountname);
					client.Dispose ();
					client = null;
				}
			}
			if (client == null) {
				taggedLog.Message ("imap connect", account.Accountname);
				//client = new ImapClient (new ProtocolLogger ("mailkit_" + account.Accountname + "_" + token + ".log"));
				client = new ImapClient ();
				account.ConnectAndAuthenticate (client: client);
				clientsCache [key] = client;
			} else {
				taggedLog.Message ("imap cached", account.Accountname);
			}
			return client;
		}

		private MessageList GetMessages (Account account, IMailFolder folder, ref Dictionary<string, MessageList> messagesCache)
		{
			if (messagesCache.ContainsKey (account.Accountname + folder.FullName)) {
				taggedLog.Message ("cached", account.Accountname, ":", folder.FullName);
				return messagesCache [account.Accountname + folder.FullName];
			} else {
				taggedLog.Message ("list", account.Accountname, ":", folder.FullName);
				return messagesCache [account.Accountname + folder.FullName] = folder.GetMessages ();
			}
		}

		private MessageList UpdateMessages (Account account, IMailFolder folder, ref Dictionary<string, MessageList> messagesCache)
		{
			taggedLog.Message ("clear cache", account.Accountname, ":", folder.FullName);
			messagesCache.Remove (account.Accountname + folder.FullName);
			return GetMessages (account, folder, ref messagesCache);
		}

		public void Sync ()
		{
			Dictionary<string, ImapClient> clientsCache = new Dictionary<string, ImapClient> ();

			foreach (Channel channel in lib.Channels) {
				ImapClient fromClient = GetClient (account: channel.FromAccount, clientsCache: ref clientsCache, token: "1");
				ImapClient toClient = GetClient (account: channel.ToAccount, clientsCache: ref clientsCache, token: "2");

				try {
					Log.Try ();

					IMailFolder[] fromFolders;
					
					if (channel.FromPath == "*") {
						taggedLog.Message ("list folders", channel.FromAccount.Accountname);
						fromFolders = fromClient.GetAllFolders (includeTrash: false).ToArray ();
						Log.Indent++;
						taggedLog.Message ("result", "all folders (except trash and spam): ", string.Join (", ", fromFolders.Select (f => f.FullName)));
						Log.Indent--;
					} else {
						fromFolders = new IMailFolder[] { fromClient.GetFolderOrCreate (path: channel.FromPath) };
					}

					Dictionary<string, MessageList> messagesCache = new Dictionary<string, MessageList> ();

					foreach (IMailFolder fromFolder in fromFolders) {
						try {
							Log.Try ();

							IMailFolder toFolder = toClient.GetFolderOrCreate (path: channel.ToPath);

							taggedLog.Message ("sync folder", channel.FromAccount.Accountname, ":", fromFolder.FullName,
								" -> ", channel.ToAccount.Accountname, ":", toFolder.FullName,
								"  (parameters: [" + string.Join (";", channel.Parameters.Select (p => p.Key + "=" + p.Value)) + "])"
							);
							Log.Indent++;

							taggedLog.Message ("open", channel.FromAccount.Accountname, ":", fromFolder.FullName);
							fromFolder.Open (FolderAccess.ReadWrite);
							int fromCount = fromFolder.Count;
							MessageList fromList = GetMessages (account: channel.FromAccount, folder: fromFolder, messagesCache: ref messagesCache);

							taggedLog.Message ("open", channel.ToAccount.Accountname, ":", toFolder.FullName);
							toFolder.Open (FolderAccess.ReadWrite);
							int toCount = toFolder.Count;
							MessageList toList = GetMessages (account: channel.ToAccount, folder: toFolder, messagesCache: ref messagesCache);

							MessageList alreadyThere = fromList.Intersect (toList);
							MessageList notThere = fromList.Except (toList);
							MessageList needToCopy = ApplyFilter (unfiltered: notThere, parameters: channel.Parameters);

							taggedLog.Message ("status", string.Join (", ", new string[] {
								"source: " + fromCount, "target: " + toCount,
								"already there: " + alreadyThere.Count, "not there: " + notThere.Count,
								"need to copy: " + needToCopy.Count, "filtered: " + (notThere.Count - needToCopy.Count)
							}));

							if (needToCopy.Count > 0) {
								if (channel.Operation == ChannelOperation.COPY) {
									CopyFolder (fromFolder, toFolder, needToCopy, channel.Parameters);
								} else if (channel.Operation == ChannelOperation.MOVE) {
									MoveFolder (fromFolder, toFolder, needToCopy, channel.Parameters);
								} else if (channel.Operation == ChannelOperation.DELETE) {
									MoveFolder (fromFolder, toFolder, needToCopy, channel.Parameters);
								}

								toFolder.Open (FolderAccess.ReadWrite);
								MessageList toListLater = UpdateMessages (account: channel.ToAccount, folder: toFolder, messagesCache: ref messagesCache);
								MessageList alreadyThereLater = fromList.Intersect (toListLater);
								MessageList notThereLater = fromList.Except (toListLater);
								MessageList needToCopyLater = ApplyFilter (unfiltered: notThereLater, parameters: channel.Parameters);
								if (needToCopyLater.Count > 0) {
									if (channel.Operation == ChannelOperation.COPY) {
										Log.Message ("Sync failed: copied: ", (needToCopy.Count - needToCopyLater.Count), ", failed to copy: ", needToCopyLater.Count);
									} else if (channel.Operation == ChannelOperation.MOVE) {
										Log.Message ("Sync failed: moved: ", (needToCopy.Count - needToCopyLater.Count), ", failed to move: ", needToCopyLater.Count);
									} else if (channel.Operation == ChannelOperation.DELETE) {
										Log.Message ("Sync failed: deleted: ", (needToCopy.Count - needToCopyLater.Count), ", failed to delete: ", needToCopyLater.Count);
									}
								}
							}

							fromFolder.Expunge ();
							toFolder.Expunge ();

							Log.Indent--;

						} catch (Exception ex) {
							Log.Error (ex);
						} finally {
							Log.Finally ();
						}
					}
				} catch (Exception ex) {
					Log.Error (ex);
				} finally {
					Log.Finally ();
				}
			}
		}
	}
}


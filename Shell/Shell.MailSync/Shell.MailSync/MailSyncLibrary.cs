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

		private void DeleteMessages (ImapFolderWrapper folder, IEnumerable<IMessageSummary> block)
		{
			taggedLog.Message ("delete block", block.First ().Print ());
			foreach (IMessageSummary other in block.Skip(1)) {
				taggedLog.Message ("", other.Print ());
			}
			folder.AddFlags (block.Select (summary => summary.UniqueId.Value), MessageFlags.Deleted, true);
		}

		private void CopyMessage (ImapFolderWrapper fromFolder, ImapFolderWrapper toFolder, IMessageSummary summary, bool verify)
		{
			CopyMessages (fromFolder: fromFolder, toFolder: toFolder, block: new IMessageSummary[]{ summary }, verify: verify);
		}

		struct CopiedMessage
		{
			public IMessageSummary SourceSummary;
			public MimeKit.MimeMessage SourceMessage;
			public UniqueId? TargetUid;
			
		};

		private void verifyMessage (ImapFolderWrapper toFolder, List<CopiedMessage> copiedMessages)
		{
			foreach (CopiedMessage copiedMessageInfo in copiedMessages) {
				MimeKit.MimeMessage message = copiedMessageInfo.SourceMessage;
				IMessageSummary summary = copiedMessageInfo.SourceSummary;
				UniqueId? uid = copiedMessageInfo.TargetUid;

				// success?
				if (uid.HasValue) {
					MimeKit.MimeMessage copiedMessage = toFolder.GetMessage (uid.Value);
					IList<IMessageSummary> copiedSummarys = toFolder.Fetch (new UniqueId[] { uid.Value }, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
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

		private void CopyMessages (ImapFolderWrapper fromFolder, ImapFolderWrapper toFolder, IEnumerable<IMessageSummary> block, bool verify)
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
				MimeKit.MimeMessage message = fromFolder.GetMessage (uid: summary.UniqueId.Value);
				Nullable<UniqueId> uid = toFolder.Append (message: message, flags: summary.Flags.HasValue ? summary.Flags.Value : MessageFlags.None, date: summary.InternalDate.Value);
				if (verify) {
					copiedMessages.Add (new CopiedMessage {
						SourceMessage = message,
						SourceSummary = summary,
						TargetUid = uid
					});
				}
			}

			if (verify) {
				verifyMessage (toFolder: toFolder, copiedMessages: copiedMessages);
			}
		}

		private bool ApplyFilter (IMessageSummary summary, Dictionary<string,string> parameters)
		{
			if (parameters.ContainsKey ("filter")) {
				string[] all = new [] {
					summary.Envelope.Subject,
					summary.Envelope.From == null ? string.Empty : string.Join (" ", summary.Envelope.From.Where (i => i != null && i.Name != null).Select (i => i.Name)),
					summary.Envelope.To == null ? string.Empty : string.Join (" ", summary.Envelope.To.Where (i => i != null && i.Name != null).Select (i => i.Name)),
					summary.SortableFrom,
					summary.SortableTo,
					
					summary.SortableCc
				}.Where (x => !string.IsNullOrWhiteSpace (x)).Select (x => x.ToLower ().Trim ()).ToArray ();

				string[] includedStrings = parameters ["filter"]
					.Split (new []{ ",", "|" }, StringSplitOptions.RemoveEmptyEntries)
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

		private void CopyFolder (ImapFolderWrapper fromFolder, ImapFolderWrapper toFolder, MessageList messages, Dictionary<string,string> parameters)
		{
			int blocksize = 10;
			int i = 0;
			int count = messages.Count;
			while (i < count) {
				IEnumerable<IMessageSummary> block = messages.Skip (i).Take (blocksize);
				i += blocksize;

				CopyMessages (fromFolder: fromFolder, toFolder: toFolder, block: block, verify: true);
			}
		}

		private void MoveFolder (ImapFolderWrapper fromFolder, ImapFolderWrapper toFolder, MessageList messages, Dictionary<string,string> parameters)
		{
			int blocksize = 10;
			int i = 0;
			int count = messages.Count;
			while (i < count) {
				IEnumerable<IMessageSummary> block = messages.Skip (i).Take (blocksize);
				i += blocksize;

				CopyMessages (fromFolder: fromFolder, toFolder: toFolder, block: block, verify: false);
				DeleteMessages (folder: fromFolder, block: block);
			}
		}

		private MessageList GetMessages (Account account, ImapFolderWrapper folder, ref Dictionary<string, MessageList> messagesCache)
		{
			if (messagesCache.ContainsKey (account.Accountname + folder.FullName)) {
				taggedLog.Message ("cached", account.Accountname, ":", folder.FullName);
				return messagesCache [account.Accountname + folder.FullName];
			} else {
				taggedLog.Message ("list", account.Accountname, ":", folder.FullName);
				return messagesCache [account.Accountname + folder.FullName] = folder.GetMessages ();
			}
		}

		private MessageList UpdateMessages (Account account, ImapFolderWrapper folder, ref Dictionary<string, MessageList> messagesCache)
		{
			taggedLog.Message ("clear cache", account.Accountname, ":", folder.FullName);
			messagesCache.Remove (account.Accountname + folder.FullName);
			return GetMessages (account, folder, ref messagesCache);
		}

		public void Sync ()
		{
			foreach (Channel channel in lib.Channels) {
				ImapClientWrapper fromClient = new ImapClientWrapper (account: channel.FromAccount, token: "1");
				ImapClientWrapper toClient = new ImapClientWrapper (account: channel.ToAccount, token: "2");

				ImapFolderWrapper[] fromFolders = new ImapFolderWrapper[0];

				try {
					Log.Try ();
					if (channel.FromPath == "*") {
						taggedLog.Message ("list folders", channel.FromAccount.Accountname);
						fromFolders = fromClient.GetAllFolders (includeTrash: false).ToArray ();
						Log.Indent++;
						taggedLog.Message ("result", "all folders (except trash and spam): ", string.Join (", ", fromFolders.Select (f => f.FullName)));
						Log.Indent--;
					} else {
						fromFolders = new ImapFolderWrapper[] { fromClient.GetFolderOrCreate (path: channel.FromPath) };
					}
				} catch (Exception ex) {
					Log.Error (ex);
				} finally {
					Log.Finally ();
				}

				Dictionary<string, MessageList> messagesCache = new Dictionary<string, MessageList> ();

				foreach (ImapFolderWrapper fromFolder in fromFolders) {
					try {
						Log.Try ();

						ImapFolderWrapper toFolder = toClient.GetFolderOrCreate (path: channel.ToPath);

						taggedLog.Message ("sync folder", channel.FromAccount.Accountname, ":", fromFolder.FullName,
							" -> ", channel.ToAccount.Accountname, ":", toFolder.FullName,
							"  (parameters: [" + string.Join (";", channel.Parameters.Select (p => p.Key + "=" + p.Value)) + "])"
						);
						Log.Indent++;

						taggedLog.Message ("open", channel.FromAccount.Accountname, ":", fromFolder.FullName);
						fromFolder.Open ();
						int fromCount = fromFolder.Count;
						MessageList fromList = GetMessages (account: channel.FromAccount, folder: fromFolder, messagesCache: ref messagesCache);

						taggedLog.Message ("open", channel.ToAccount.Accountname, ":", toFolder.FullName);
						toFolder.Open ();
						int toCount = toFolder.Count;
						MessageList toList = GetMessages (account: channel.ToAccount, folder: toFolder, messagesCache: ref messagesCache);

						MessageList alreadyThere = fromList.Intersect (toList);
						MessageList notThere = fromList.Except (toList);
						MessageList needToCopy = ApplyFilter (unfiltered: notThere, parameters: channel.Parameters);

						taggedLog.Message ("status", string.Join (", ", new [] {
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

							toFolder.Open ();
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
			}
		}
	}
}


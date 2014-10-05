using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
	public class MailStatusTask : ScriptTask, MainScriptTask
	{
		public MailStatusTask ()
		{
			Name = "MailSync";
			Description = "Show status of all e-mail folders";
			Options = new string[] { "mail-status" };
		}

		protected override void InternalRun (string[] args)
		{
			MailLibrary lib = new MailLibrary (filesystems: fs);
			lib.UpdateConfigs ();
			MailSyncLibrary syncLib = new MailSyncLibrary (lib);
			syncLib.ListAccounts ();
			syncLib.ListFolders ();
		}
	}
}


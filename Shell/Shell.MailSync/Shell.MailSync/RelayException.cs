using System;

namespace Shell.MailSync
{
	public class RelayException : Exception
	{
		public RelayException (Exception exception)
			: base ("Relay: [" + exception.Message + "]")
		{
		}

		public RelayException (string message)
			: base ("Relay: [" + message + "]")
		{
		}
	}
}


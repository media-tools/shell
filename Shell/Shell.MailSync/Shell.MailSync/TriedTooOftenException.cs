using System;

namespace Shell.MailSync
{
	public class TriedTooOftenException : Exception
	{
		public TriedTooOftenException (Exception exception)
			: base ("Tried too often: [" + exception.Message + "]")
		{
		}

		public TriedTooOftenException (string message)
			: base ("Tried too often: [" + message + "]")
		{
		}
	}
}


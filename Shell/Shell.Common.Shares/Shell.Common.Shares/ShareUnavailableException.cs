using System;

namespace Shell.Common.Shares
{
    public class ShareUnavailableException : ArgumentException
    {
        public ShareUnavailableException (string message)
            : base (message)
        {
        }
    }
}


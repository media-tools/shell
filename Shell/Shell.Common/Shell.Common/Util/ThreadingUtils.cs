using System;
using System.Threading;

namespace Shell.Common
{
    public class ThreadingUtils
    {
        public static bool CallWithTimeout (Action action, int timeoutMilliseconds)
        {
            Thread threadToKill = null;
            Action wrappedAction = () => {
                threadToKill = Thread.CurrentThread;
                action ();
            };

            IAsyncResult result = wrappedAction.BeginInvoke (null, null);
            if (result.AsyncWaitHandle.WaitOne (timeoutMilliseconds)) {
                wrappedAction.EndInvoke (result);
                return true;
            } else {
                threadToKill.Abort ();
                return false;
            }
        }
    }
}


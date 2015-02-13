using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shell.Common.IO;

namespace Shell.Common.Util
{
    public static class CustomParallel
    {
        public static void ForEach <T> (IEnumerable<T> source, Action<T> body)
        {
            T[] sourceArray = source.ToArray ();
            int threadCount = (int)(Math.Min (25.0, Math.Max (2.0, Math.Sqrt (sourceArray.Length) * 2)));
            Log.Debug ("CustomParallel: count of threads = ", threadCount);

            int sourceIndex = 0;
            int threadIndex = 0;
            object lockableObject = new object ();
            Action bodyWrapper = () => {
                int currentThreadIndex;
                lock (lockableObject) {
                    currentThreadIndex = threadIndex++;
                }

                int currentSourceIndex;
                while (true) {
                    lock (lockableObject) {
                        currentSourceIndex = sourceIndex++;
                    }
                    if (currentSourceIndex < sourceArray.Length) {
                        //Log.Debug ("CustomParallel: thread #", currentThreadIndex, ": get element ", currentSourceIndex, " of ", sourceArray.Length);
                        body (sourceArray [currentSourceIndex]);
                    } else {
                        break;
                    }
                }
            };

            Thread[] threadArray = new Thread[threadCount];
            Log.Debug ("CustomParallel: initialize threads...");
            for (int i = 0; i < threadArray.Length; ++i) {
                threadArray [i] = new Thread (() => bodyWrapper ());
            }
            Log.Debug ("CustomParallel: start threads...");
            for (int i = 0; i < threadArray.Length; ++i) {
                threadArray [i].Start ();
            }
            Log.Debug ("CustomParallel: join threads...");
            for (int i = 0; i < threadArray.Length; ++i) {
                threadArray [i].Join ();
            }
        }
    }
}


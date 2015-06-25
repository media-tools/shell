using System;
using System.Collections.Generic;
using Shell.Common.IO;

namespace Shell.Common.Util
{
    public static class Actions
    {
        public static void Empty ()
        {
        }

        public static void Empty<T> (T value)
        {
        }

        public static void Empty<T1, T2> (T1 value1, T2 value2)
        {
        }
        /* Put as many overloads as you want */

        public static void AddOrExecuteNow (this List<Action> delayedOperations, Action operation, string logHeader = null)
        {
            if (operation != null) {
                if (delayedOperations != null) {
                    if (logHeader != null) {
                        Action originalOperation = operation;
                        operation = () => {
                            Log.Info (logHeader);
                            Log.Indent++;
                            originalOperation ();
                            Log.Indent--;
                        };
                    }
                    delayedOperations.Add (operation);
                } else {
                    operation ();
                }
            }
        }
    }

    public static class Functions
    {
        public static T Identity<T> (T value)
        {
            return value;
        }

        public static T0 Default<T0> ()
        {
            return default(T0);
        }

        public static T0 Default<T1, T0> (T1 value1)
        {
            return default(T0);
        }
        /* Put as many overloads as you want */
    }
}


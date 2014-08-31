using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using Control.Common.Tasks;
using Control.Common.Util;
using Control.Common.IO;
using Control.Common.Hooks;
using Control.Common;

namespace Control
{
    public class MainClass
    {
        private static IEnumerable<Task> mainTasks;

        public static void Main (string[] args)
        {
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                Log.MessageConsole ();
                Log.MessageLog ("Cancelled!");
                e.Cancel = false;
            };

            Log.Init (program: "control", version: Commons.VERSION_STR);

            IEnumerable<Assembly> asses = ReflectiveEnumerator.LoadAssemblies ();
            mainTasks = from ass in asses
                                 from type in ReflectiveEnumerator.FindClassImplementingInterface<MainTask> (ass)
                                 select type as Task;
            foreach (Hook hook in from ass in asses from type in ReflectiveEnumerator.FindSubclasses<Hook>(ass) select type as Hook) {
                Program.HooksBeforeTask += task => hook.HookBeforeTask (task);
                Program.HooksAfterTask += task => hook.HookAfterTask (task);
            }

            try {
                new Program (mainTasks).Main (args);
            } catch (Exception ex) {
                Log.Error (ex);
            }
        }
    }

}

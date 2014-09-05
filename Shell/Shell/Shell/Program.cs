using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Common.IO;
using Shell.Common.Hooks;
using Shell.Common;

namespace Shell
{
    public class MainClass
    {
        private static IEnumerable<Task> mainTasks;

        public static void Main (string[] args)
        {
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                Commons.OnCancel ();
                e.Cancel = false;
            };

            Log.Init (program: "shell", version: Commons.VERSION_STR);

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

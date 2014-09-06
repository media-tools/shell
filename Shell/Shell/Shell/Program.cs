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
        private static IEnumerable<ScriptTask> mainTasks;

        public static void Main (string[] args)
        {
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                Commons.OnCancel ();
                e.Cancel = false;
            };

            Log.Init (program: "shell", version: Commons.VERSION_STR);

            IEnumerable<Assembly> asses = ReflectiveEnumerator.LoadAssemblies ();
            mainTasks = from ass in asses
                                 from type in ReflectiveEnumerator.FindClassImplementingInterface<MainScriptTask> (ass)
                                 select type as ScriptTask;
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

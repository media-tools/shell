using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Options;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Git
{
    public class InstallTask : ScriptTask, MainScriptTask
    {
        public InstallTask ()
        {
            Name = "Install";
            Description = new [] { "Install" };
            Options = new [] { "install" };
        }

        protected override void InternalRun (string[] args)
        {
            // write the bash completion config file
            IEnumerable<string> options = from task in Program.Tasks
                                                   from option in task.Options
                                                   select option;
            string bash_completion = string.Join ("\n", new [] {
                "_dotnetshell_completion() {",
                "    local cur=${COMP_WORDS[COMP_CWORD]};",
                "    prev=${COMP_WORDS[COMP_CWORD-1]}",
                "    local frst=${COMP_WORDS[1]}",
                "",
                "    COMPREPLY=( $(compgen -W \"" + string.Join (" ", options) + "\" -- $cur) );",
                "",
                "    case \"${frst}\" in",
                ""
            });
            foreach (ScriptTask task in Program.Tasks) {
                string taskOptions = string.Join (" | ", from o in task.Options
                                                                     select "'" + o + "'");
                string taskParams;
                if (task is MonoOptionsScriptTask) {
                    taskParams = string.Join (" ", (task as MonoOptionsScriptTask).OptionSetParameters);
                } else {
                    taskParams = string.Join (" ", from p in task.ParameterSyntax
                                                                  where !p.StartsWith ("[")
                                                                  select p.Split (' ') [0]);
                }
                bash_completion += "    " + taskOptions + " )\n        COMPREPLY=( $(compgen -W \"" + taskParams + "\" -- ${cur}) )\n        return 0;;\n";
            }
            bash_completion += string.Join ("\n", new [] {
                "    esac",
                "}",
                "complete -F _dotnetshell_completion cs dotnetshell",
                "complete -F _dotnetshell_completion cs-debug dotnetshell",
                ""
            });
            fs.Runtime.WriteAllText (path: "bash_completion.sh", contents: bash_completion);

            // write the install script
            string script = "";
            script += "mkdir /opt/dotnetshell/ 2>/dev/null\n";
            // soft-link the executables
            script += "ln -sf " + Commons.EXE_DIRECTORY + "/*.{exe,dll} /opt/dotnetshell/\n";
            // install the shell script wrappers
            script += "echo '/opt/dotnetshell/" + Commons.EXE_FILENAME + " \"$@\"' > /opt/dotnetshell/dotnetshell.sh\n";
            script += "echo 'mono --debug /opt/dotnetshell/" + Commons.EXE_FILENAME + " \"$@\"' > /opt/dotnetshell/dotnetshell-debug.sh\n";
            script += "chmod 755 /opt/dotnetshell/*\n";
            script += "ln -sf /opt/dotnetshell/dotnetshell.sh /usr/local/bin/dotnetshell\n";
            script += "ln -sf /opt/dotnetshell/dotnetshell.sh /usr/local/bin/cs\n";
            script += "ln -sf /opt/dotnetshell/dotnetshell-debug.sh /usr/local/bin/dotnetshell-debug\n";
            script += "ln -sf /opt/dotnetshell/dotnetshell-debug.sh /usr/local/bin/cs-debug\n";
            script += "cp -f " + fs.Runtime.RootDirectory + "/bash_completion.sh /etc/bash_completion.d/dotnetshell\n";

            // run the install script
            fs.Runtime.WriteAllText (path: "install.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "install.sh", sudo: true);

            Log.Message ("source /etc/bash_completion.d/dotnetshell");
        }
    }
}


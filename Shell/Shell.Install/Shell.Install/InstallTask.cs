using System;
using System.Collections.Generic;
using Shell.Common;
using System.Linq;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Git
{
    public class InstallTask : ScriptTask, MainScriptTask
    {
        public InstallTask ()
        {
            Name = "Install";
            Description = "Install";
            Options = new string[] { "install" };
        }

        protected override void InternalRun (string[] args)
        {
            // write the bash completion config file
            IEnumerable<string> options = from task in Program.Tasks from option in task.Options select option;
            string bash_completion = "_dotnetshell_completion() {     local cur=${COMP_WORDS[COMP_CWORD]};     COMPREPLY=( $(compgen -W \"" + string.Join (" ", options) + "\" -- $cur) ); }\n";
            bash_completion += "complete -F _dotnetshell_completion cs dotnetshell\n";
            fs.Runtime.WriteAllText (path: "bash_completion.sh", contents: bash_completion);

            // write the install script
            string script = "";
            script += "mkdir /opt/dotnetshell/ 2>/dev/null\n";
            // soft-link the executables
            script += "ln -sf " + Commons.EXE_DIRECTORY + "/*.{exe,dll} /opt/dotnetshell/\n";
            // install the shell script wrapper
            script += "echo '/opt/dotnetshell/" + Commons.EXE_FILENAME + " \"$@\"' > /opt/dotnetshell/dotnetshell.sh\n";
            script += "chmod 755 /opt/dotnetshell/*\n";
            script += "ln -sf /opt/dotnetshell/dotnetshell.sh /usr/local/bin/dotnetshell\n";
            script += "ln -sf /opt/dotnetshell/dotnetshell.sh /usr/local/bin/cs\n";
            script += "cp -f " + fs.Runtime.RootDirectory + "/bash_completion.sh /etc/bash_completion.d/dotnetshell\n";

            // run the install script
            fs.Runtime.WriteAllText (path: "install.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "install.sh", sudo: true);
        }
    }
}


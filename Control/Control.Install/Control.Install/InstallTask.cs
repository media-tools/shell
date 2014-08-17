using System;
using System.Collections.Generic;
using Control.Common;
using System.Linq;
using Control.Common.Tasks;
using Control.Common.Util;

namespace Control.Git
{
    public class InstallTask : Task, MainTask
    {
        public InstallTask ()
        {
            Name = "Install";
            Description = "Install this shit";
            Options = new string[] { "install" };
        }

        protected override void InternalRun (string[] args)
        {
            // write the bash completion config file
            IEnumerable<string> options = from task in Program.Tasks from option in task.Options select option;
            string bash_completion = "_control_completion() {     local cur=${COMP_WORDS[COMP_CWORD]};     COMPREPLY=( $(compgen -W \"" + string.Join (" ", options) + "\" -- $cur) ); }\n";
            bash_completion += "complete -F _control_completion cl control\n";
            fs.Runtime.WriteAllText (path: "bash_completion.sh", contents: bash_completion);

            // write the install script
            string script = "";
            script += "mkdir /opt/control/ 2>/dev/null\n";
            // soft-link the executables
            script += "ln -sf " + Commons.EXE_DIRECTORY + "/*.{exe,dll} /opt/control/\n";
            // install the shell script wrapper
            script += "echo '/opt/control/" + Commons.EXE_FILENAME + " \"$@\"' > /opt/control/control.sh\n";
            script += "chmod 755 /opt/control/*\n";
            script += "ln -sf /opt/control/control.sh /usr/local/bin/control\n";
            script += "ln -sf /opt/control/control.sh /usr/local/bin/cl\n";
            script += "cp -f " + fs.Runtime.RootDirectory + "/bash_completion.sh /etc/bash_completion.d/control\n";

            // run the install script
            fs.Runtime.WriteAllText (path: "install.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "install.sh", sudo: true);
        }
    }
}


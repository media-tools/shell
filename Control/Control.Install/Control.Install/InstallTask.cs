using System;
using Control.Common;

namespace Control.Git
{
    public class InstallTask : Task
    {
        public InstallTask ()
        {
            Name = "Install";
            Description = "Install this shit";
            Options = new string[] { "install" };
        }

        protected override void InternalRun (string[] args)
        {
            string script = "";
            script += "mkdir /opt/control/ 2>/dev/null\n";
            script += "ln -sf " + Commons.EXE_DIRECTORY + "/*.{exe,dll} /opt/control/\n";
            script += "echo '/opt/control/" + Commons.EXE_FILENAME + " \"$@\"' > /opt/control/control.sh\n";
            script += "chmod 755 /opt/control/*\n";
            script += "ln -sf /opt/control/control.sh /usr/local/bin/cl\n";
            fs.Runtime.WriteAllText (path: "install.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "install.sh", sudo: true);
        }
    }
}


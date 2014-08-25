using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace Control.Compatibility
{
    public class SystemHelper
    {
        public static SystemHelper Instance {
            get;
            set;
        }

        static SystemHelper ()
        {
            if (Environment.OSVersion.Platform.ToString ().StartsWith ("Win")) {
                Instance = new SystemHelper ();
            } else {
                var ufh = Type.GetType ("Control.Compatibility.Linux.LinuxSystemHelper");
                if (ufh == null) {
                    ufh = Type.GetType ("Control.Compatibility.Linux.LinuxSystemHelper, Control.Compatibility.Linux");
                }
                if (ufh == null) {
                    string directory = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location);
                    Assembly assembly = Assembly.LoadFile (Path.Combine (directory, "Control.Compatibility.Linux.dll"));
                    if (assembly == null)
                        throw new Exception ("Control.Compatibility.Linux.dll is required when running on a Linux based system");
                    ufh = assembly.GetType ("Control.Compatibility.Linux.LinuxSystemHelper");
                }
                Instance = (SystemHelper)Activator.CreateInstance (ufh);
            }
        }

        public virtual uint GetUid ()
        {
            return (uint)Environment.UserName.GetHashCode ();
        }
    }
}


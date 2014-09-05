using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace Shell.Compatibility
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
                var ufh = Type.GetType ("Shell.Compatibility.Linux.LinuxSystemHelper");
                if (ufh == null) {
                    ufh = Type.GetType ("Shell.Compatibility.Linux.LinuxSystemHelper, Shell.Compatibility.Linux");
                }
                if (ufh == null) {
                    string directory = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location);
                    Assembly assembly = Assembly.LoadFile (Path.Combine (directory, "Shell.Compatibility.Linux.dll"));
                    if (assembly == null)
                        throw new Exception ("Shell.Compatibility.Linux.dll is required when running on a Linux based system");
                    ufh = assembly.GetType ("Shell.Compatibility.Linux.LinuxSystemHelper");
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


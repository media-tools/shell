using System;
using System.IO;
using System.Reflection;

namespace Control.Compatibility
{
    public class FileHelper
    {
        public static FileHelper Instance {
            get;
            set;
        }

        static FileHelper ()
        {
            if (Environment.OSVersion.Platform.ToString ().StartsWith ("Win")) {
                Instance = new FileHelper ();
            } else {
                var ufh = Type.GetType ("Control.Compatibility.Linux.LinuxFileHelper");
                if (ufh == null) {
                    ufh = Type.GetType ("Control.Compatibility.Linux.LinuxFileHelper, Control.Compatibility.Linux");
                }
                if (ufh == null) {
                    string directory = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location);
                    Assembly assembly = Assembly.LoadFile (Path.Combine (directory, "Control.Compatibility.Linux.dll"));
                    if (assembly == null)
                        throw new Exception ("Control.Compatibility.Linux.dll is required when running on a Linux based system");
                    ufh = assembly.GetType ("Control.Compatibility.Linux.LinuxFileHelper");
                }
                Instance = (FileHelper)Activator.CreateInstance (ufh);
            }
        }

        public virtual bool IsSymLink (FileInfo file)
        {
            return IsSymLink (file.FullName);
        }

        public virtual bool IsSymLink (DirectoryInfo directory)
        {
            return IsSymLink (directory.FullName);
        }

        public virtual bool IsSymLink (string path)
        {
            return false;
        }
    }
}


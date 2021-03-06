using System;
using System.Collections.Generic;
using System.Reflection;
using Shell.Common;
using System.Linq;
using System.IO;
using Shell.Common.IO;

namespace Shell.Common.Util
{
    public static class ReflectiveEnumerator
    {
        static ReflectiveEnumerator ()
        {
        }

        public static Assembly[] LoadAssemblies ()
        {
            string directory = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location);
            IEnumerable<string> files = Directory.EnumerateFiles (path: directory);
            List<Assembly> assemblies = new List<Assembly> ();
            foreach (string file in files) {
                if (file.ToLower ().EndsWith ("dll") && Path.GetFileName (file).ToLower ().Contains ("shell")) {
                    assemblies.Add (Assembly.LoadFrom (file));
                }
            }
            return assemblies.ToArray ();
        }

        public static IEnumerable<T> FindSubclasses<T> (Assembly assembly = null) where T : class
        {
            assembly = assembly ?? Assembly.GetAssembly (typeof(T));
            List<T> objects = new List<T> ();
            try {
                foreach (Type type in assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)))) {
                    //Console.WriteLine (type + "\t" + typeof(T).IsAssignableFrom (type));
                    objects.Add ((T)Activator.CreateInstance (type));
                }
            } catch (ReflectionTypeLoadException ex) {
                Log.Info ("Error in assembly: ", assembly.FullName);
                Log.Error (ex);
            }
            return objects;
        }

        public static IEnumerable<T> FindClassImplementingInterface<T> (Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetAssembly (typeof(T));
            List<T> objects = new List<T> ();
            try {
                foreach (Type type in assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && typeof(T).IsAssignableFrom(myType))) {
                    //Console.WriteLine (type + "\t" + typeof(T).IsAssignableFrom (type));
                    objects.Add ((T)Activator.CreateInstance (type));
                }
            } catch (ReflectionTypeLoadException ex) {
                Log.Info ("Error in assembly: ", assembly.FullName);
                Log.Error (ex);
            }
            return objects;
        }
    }
}

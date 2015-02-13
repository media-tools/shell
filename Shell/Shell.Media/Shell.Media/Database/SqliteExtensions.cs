using System;
using SQLite;
using System.Collections.Generic;

namespace Shell.Media
{
    public static class SqliteExtensions
    {
        /*
        public static void InsertOrIgnoreWithChildren (this SQLiteConnection conn, object element, bool recursive = false)
        {
            InsertOrIgnoreWithChildrenRecursive (conn, element, recursive);
        }


        static void InsertOrIgnoreWithChildrenRecursive (this SQLiteConnection conn, object element, bool recursive, ISet<object> objectCache = null)
        {
            objectCache = objectCache ?? new HashSet<object> ();
            if (objectCache.Contains (element))
                return;

            conn.Insert (element, "OR IGNORE", objectCache);

            objectCache.Add (element);
            conn.InsertChildrenRecursive (element, replace, recursive, objectCache);

            conn.UpdateWithChildren (element);
        }
*/
    }
}


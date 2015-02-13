using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.IO;

namespace Shell.Media.Database
{
    public interface IDatabaseAware
    {
        MediaDatabase Database { get; set; }

        void AssignDatabase (MediaDatabase database);
    }

    public static class DatabaseAwareness
    {
        public static IEnumerable<T> AssignDatabase<T> (this IEnumerable<T> enumerable, MediaDatabase database)
            where T : IDatabaseAware
        {
            foreach (T elem in enumerable) {
                elem.Database = database;
                yield return elem;
            }
        }

        private static Dictionary<Type, MediaDatabase> autoAssignableDatabases = new Dictionary<Type, MediaDatabase> ();

        public static void SetDatabase<T> (MediaDatabase database)
        {
            autoAssignableDatabases [typeof(T)] = database;
        }

        public static void AutoAssignDatabase<T> (T fromSql)
            where T : IDatabaseAware
        {
            Type type = fromSql.GetType ();
            if (autoAssignableDatabases.ContainsKey (type)) {
                fromSql.AssignDatabase (autoAssignableDatabases [type]);

            } else {
                string err = Log.FormatString ("Unable to auto assign database: type=", typeof(T).Name,
                                 ", available=", string.Join (",", autoAssignableDatabases.Keys.Select (k => k.Name)));
                throw new ArgumentException (err);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Control.Common.Util
{
    public static class CollectionExtensions
    {
        public static void AddRange <A> (this List<A> list, params A[] items)
        {
            list.AddRange ((IEnumerable<A>)items);
        }

        public static string[] SplitValues (this string str)
        {
            return str.Split (new string[]{ ";" }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string JoinValues (this IEnumerable<string> values)
        {
            return string.Join (";", new HashSet<string> (values));
        }

        public static IEnumerable<A> Concat <A> (this IEnumerable<A> enumerable, A value)
        {
            return enumerable.Concat (new A[]{ value });
        }

        public static HashSet<A> ToHashSet <A> (this IEnumerable<A> values)
        {
            return new HashSet<A> (values);
        }

        public static void ForEach <A> (this IEnumerable<A> enumerable, Action<A> action)
        {
            foreach (A obj in enumerable) {
                action (obj);
            }
        }

        public static string RemoveNonAlphanumeric (this string text)
        {
            StringBuilder result = new StringBuilder (text.Length);

            foreach (char c in text) {
                if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9') {
                    result.Append (c);
                }
            }

            return result.ToString ();
        }
    }
}


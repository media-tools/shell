using System;
using System.Linq;
using System.Collections.Generic;

namespace Shell.Common.Util
{
    public class Filter
    {
        private string[] Elements = null;
        private FilterType Type;

        private Filter ()
        {
        }

        public static Filter None { get { return new Filter () { Type = FilterType.ACCEPT_EVERYTHING }; } }

        public static Filter ContainFilter (params string[] filter)
        {
            return new Filter () {
                Type = FilterType.CONTAINS,
                Elements = filter
            };
        }

        public static Filter ExactFilter (params string[] filter)
        {
            return new Filter () {
                Type = FilterType.EXACT_MATCH,
                Elements = filter
            };
        }

        public bool Matches (IFilterable filterable)
        {
            return Matches (filterable.FilterKeys ());
        }

        public bool Matches (params string[] possibleMatches)
        {
            switch (Type) {
            case FilterType.ACCEPT_EVERYTHING:
                return true;
            
            case FilterType.CONTAINS:
                foreach (string possibleMatch in possibleMatches) {
                    if (Elements.Any (f => possibleMatch.Contains (f, StringComparison.OrdinalIgnoreCase))) {
                        return true;
                    }
                }
                return false;

            case FilterType.EXACT_MATCH:
                foreach (string possibleMatch in possibleMatches) {
                    if (Elements.Any (f => possibleMatch.ToLower () == f.ToLower ())) {
                        return true;
                    }
                }
                return false;

            default:
                throw new InvalidOperationException ("Filter has no type! (" + this + ")");
            }
        }

        public static string[] Split (string serialized)
        {
            return serialized.Split (new char[]{ ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        protected enum FilterType
        {
            ACCEPT_EVERYTHING,
            CONTAINS,
            EXACT_MATCH
        }
    }

    public static class FilterExtensions
    {
        public static IEnumerable<A> Filter <A> (this IEnumerable<A> collection, Filter filter) where A : IFilterable
        {
            foreach (A element in collection) {
                if (filter.Matches (element)) {
                    yield return element;
                }
            }
        }

        public static IEnumerable<A> FilterBy <A, B> (this IEnumerable<A> collection, Filter filter, Func<A, B> by) where B : IFilterable
        {
            foreach (A element in collection) {
                if (filter.Matches (by (element))) {
                    yield return element;
                }
            }
        }

        public static A[] Filter <A> (this A[] array, Filter filter) where A : IFilterable
        {
            return Filter ((IEnumerable<A>)array, filter).ToArray ();
        }

        public static A[] FilterBy <A, B> (this A[] array, Filter filter, Func<A, B> by) where B : IFilterable
        {
            return FilterBy ((IEnumerable<A>)array, filter, by).ToArray ();
        }
    }

    public interface IFilterable
    {
        string[] FilterKeys ();
    }
}


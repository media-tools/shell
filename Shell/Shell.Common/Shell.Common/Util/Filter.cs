using System;
using System.Linq;
using System.Collections.Generic;

namespace Shell.Common.Util
{
    public class Filter
    {
        private string[] Elements = null;
        private FilterType Type;
        private Filter[] SubFilters = null;

        private Filter ()
        {
        }

        public static Filter None { get { return new Filter () { Type = FilterType.ACCEPT_EVERYTHING }; } }

        public static Filter ContainFilter (params string[] filter)
        {
            if (filter.Length > 0) {
                return new Filter () {
                    Type = FilterType.CONTAINS,
                    Elements = filter.ToArray ()
                };
            } else {
                return Filter.None;
            }
        }

        public static Filter ExactFilter (Filter copyFrom)
        {
            if (copyFrom.Type == FilterType.CONTAINS || copyFrom.Type == FilterType.EXACT_MATCH) {
                return new Filter () {
                    Type = FilterType.EXACT_MATCH,
                    Elements = copyFrom.Elements.ToArray ()
                };
            } else {
                throw new ArgumentException ("Invalid filter conversion: from " + copyFrom.Type.ToString () + " to EXACT_MATCH");
            }
        }

        public static Filter ExactFilter (params string[] filter)
        {
            if (filter.Length > 0) {
                return new Filter () {
                    Type = FilterType.EXACT_MATCH,
                    Elements = filter.ToArray ()
                };
            } else {
                return Filter.None;
            }
        }

        public static Filter And (params Filter[] filters)
        {
            if (filters.Length > 0) {
                return new Filter () {
                    Type = FilterType.COMBINED_FILTER_AND,
                    SubFilters = filters.ToArray ()
                };
            } else {
                return Filter.None;
            }
        }

        public static Filter Or (params Filter[] filters)
        {
            if (filters.Length > 0) {
                return new Filter () {
                    Type = FilterType.COMBINED_FILTER_OR,
                    SubFilters = filters.ToArray ()
                };
            } else {
                return Filter.None;
            }
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

            case FilterType.COMBINED_FILTER_AND:
                {
                    bool match = true;
                    foreach (Filter subFilter in SubFilters) {
                        match = match && subFilter.Matches (possibleMatches);
                        if (!match)
                            break;
                    }
                    return match;
                }

            case FilterType.COMBINED_FILTER_OR:
                {
                    bool match = false;
                    foreach (Filter subFilter in SubFilters) {
                        match = match || subFilter.Matches (possibleMatches);
                        if (match)
                            break;
                    }
                    return match;
                }

            default:
                throw new InvalidOperationException ("Filter has no type! (" + this + ")");
            }
        }

        public static string[] Split (string serialized)
        {
            return serialized.Split (new char[]{ ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool AcceptsEverything { get { return Type == FilterType.ACCEPT_EVERYTHING; } }

        protected enum FilterType
        {
            ACCEPT_EVERYTHING,
            CONTAINS,
            EXACT_MATCH,
            COMBINED_FILTER_AND,
            COMBINED_FILTER_OR,
        }


        public override string ToString ()
        {
            switch (Type) {
            case FilterType.ACCEPT_EVERYTHING:
                return "{ * }";

            case FilterType.CONTAINS:
                return "{ contains: [" + string.Join (", ", Elements.Select (e => "\"" + e + "\"")) + "] }";

            case FilterType.EXACT_MATCH:
                return "{ exact: [" + string.Join (", ", Elements.Select (e => "\"" + e + "\"")) + "] }";

            case FilterType.COMBINED_FILTER_AND:
                return "{ and: [" + string.Join (", ", SubFilters.Select (e => e.ToString ())) + "] }";

            case FilterType.COMBINED_FILTER_OR:
                return "{ or: [" + string.Join (", ", SubFilters.Select (e => e.ToString ())) + "] }";

            default:
                throw new InvalidOperationException ("Filter has no type! (" + this + ")");
            }
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


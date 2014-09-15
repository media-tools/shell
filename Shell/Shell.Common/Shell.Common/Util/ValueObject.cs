using System;
using System.Collections.Generic;
using System.Linq;

namespace Shell.Common.Util
{
    public abstract class ValueObject<T> : IEquatable<T>
        where T : ValueObject<T>
    {
        protected abstract IEnumerable<object> Reflect ();

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (null, obj))
                return false;
            if (obj.GetType () != GetType ())
                return false;
            return Equals (obj as T);
        }

        public bool Equals (T other)
        {
            if (ReferenceEquals (null, other))
                return false;
            if (ReferenceEquals (this, other))
                return true;
            return Reflect ().SequenceEqual (other.Reflect ());
        }

        public override int GetHashCode ()
        {
            return Reflect ().Aggregate (36, (hashCode, value) => value == null ?
                hashCode : hashCode ^ value.GetHashCode ());
        }

        public override string ToString ()
        {
            return "{ " + Reflect ().Aggregate ((l, r) => l + ", " + r) + " }";
        }

        public static bool Equality (T a, T b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals (a, b)) {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }

            // Return true if the fields match:
            return a.Reflect ().SequenceEqual (b.Reflect ());
        }

        public static bool Inequality (T a, T b)
        {
            return !(Equality (a, b));
        }
    }
}


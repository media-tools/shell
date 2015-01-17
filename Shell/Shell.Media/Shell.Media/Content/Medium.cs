using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Media.Files;
using System.Collections.Generic;

namespace Shell.Media.Content
{
    public abstract class Medium : ValueObject<Medium>
    {
        protected static MediaFileLibrary libMediaFile = new MediaFileLibrary ();

        public HexString Hash { get; private set; }

        public abstract string Type { get; }

        public string MimeType { get; protected set; }

        public Medium (HexString hash)
        {
            Hash = hash;
        }

        public abstract void Index (string fullPath);

        public abstract bool IsCompletelyIndexed { get; }

        public bool IsDeleted { get; set; }

        public abstract DateTime? PreferredTimestamp { get; }

        public Dictionary<string, string> Serialize ()
        {
            Dictionary<string, string> dict = new Dictionary<string, string> ();

            // save the mime type
            dict ["file:MimeType"] = MimeType;

            SerializeInternal (dict: dict);

            return dict;
        }

        public void Deserialize (Dictionary<string, string> dict)
        {
            // load the mime type
            MimeType = dict.ContainsKey ("file:MimeType") ? dict ["file:MimeType"] : null;

            DeserializeInternal (dict: dict);
        }

        protected abstract void SerializeInternal (Dictionary<string, string> dict);

        protected abstract void DeserializeInternal (Dictionary<string, string> dict);

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Hash };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<Medium>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (Medium a, Medium b)
        {
            return ValueObject<Medium>.Equality (a, b);
        }

        public static bool operator != (Medium a, Medium b)
        {
            return ValueObject<Medium>.Inequality (a, b);
        }
    }
}


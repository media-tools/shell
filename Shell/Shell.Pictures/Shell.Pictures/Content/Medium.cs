﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Pictures.Files;
using System.Collections.Generic;

namespace Shell.Pictures.Content
{
    public abstract class Medium : ValueObject<Medium>
    {
        public HexString Hash { get; private set; }

        public abstract string Type { get; }

        public HashSet<MediaFile> Files = new HashSet<MediaFile> ();

        public Medium (HexString hash)
        {
            Hash = hash;
        }

        public Medium (string fullPath)
        {
            Hash = FileSystemUtilities.HashOfFile (path: fullPath);
        }

        public void AddFile (MediaFile mediaFile)
        {
            Files.Add (mediaFile);
        }

        public virtual Dictionary<string, string> Serialize ()
        {
            throw new NotImplementedException ();
        }

        public virtual void Deserialize (Dictionary<string, string> dict)
        {
            throw new NotImplementedException ();
        }

        protected override IEnumerable<object> Reflect()
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

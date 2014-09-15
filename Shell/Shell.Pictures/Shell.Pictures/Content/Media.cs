using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Pictures.Files;
using System.Collections.Generic;

namespace Shell.Pictures.Content
{
    [JsonConverter (typeof(MediaConverter))]
    public abstract class Media : ValueObject<Media>
    {
        public HexString Hash { get; private set; }

        public abstract string Type { get; }

        public HashSet<MediaFile> Files = new HashSet<MediaFile> ();

        public Media (HexString hash)
        {
            Hash = hash;
        }

        public Media (string fullPath)
        {
            Hash = FileSystemUtilities.HashOfFile (path: fullPath);
        }

        public void AddFile (MediaFile mediaFile)
        {
            Files.Add (mediaFile);
        }

        public virtual void WriteAttributes (JsonWriter writer, JsonSerializer serializer)
        {
            throw new NotImplementedException ();
        }

        public virtual void ReadAttributes (JObject jsonObject)
        {
            throw new NotImplementedException ();
        }

        protected override IEnumerable<object> Reflect()
        {
            return new object[] { Hash };
        }

        public static bool operator == (Media c1, Media c2)
        {
            return ValueObject<Media>.Equality (c1, c2);
        }

        public static bool operator != (Media c1, Media c2)
        {
            return ValueObject<Media>.Inequality (c1, c2);
        }
    }

    public sealed class MediaConverter : JsonConverter
    {
        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            Media media = (Media)value;
            writer.WriteStartObject ();
            writer.WritePropertyName ("Hash");
            writer.WriteValue (media.Hash.Hash);
            writer.WritePropertyName ("Type");
            writer.WriteValue (media.Type);
            media.WriteAttributes (writer, serializer);
            writer.WriteEndObject ();
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load (reader);
            HexString hash = new HexString { Hash = (string)jsonObject.Property ("Hash") };
            string type = (string)jsonObject.Property ("Type");
            Media media;
            if (type == typeof(Picture).ToString ()) {
                media = new Picture (hash: hash);
            } else if (type == typeof(Audio).ToString ()) {
                media = new Audio (hash: hash);
            } else if (type == typeof(Video).ToString ()) {
                media = new Video (hash: hash);
            } else {
                throw new NotImplementedException ("Unknown type: " + objectType);
            }
            media.ReadAttributes (jsonObject: jsonObject);

            return media;
        }

        public override bool CanConvert (Type objectType)
        {
            return typeof(Media).IsAssignableFrom (objectType);
        }
    }
}


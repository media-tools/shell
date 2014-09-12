using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Shell.Common.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Shell.Pictures
{
    [JsonConverter (typeof(MediaFileConverter))]
    public abstract class MediaFile
    {
        public string FullPath { get; private set; }

        public string Name { get; private set; }

        public string Extension { get; private set; }

        public string RelativePath { get; private set; }

        public string AlbumPath { get; private set; }

        public string Root { get; private set; }

        public MediaFile (string fullPath, string root)
        {
            Debug.Assert (fullPath.StartsWith (root), "file path is not in root directory (FullName=" + fullPath + ",root=" + root + ")");
            Root = root;
            FullPath = fullPath;
            Name = Path.GetFileName (fullPath);
            Extension = Path.GetExtension (fullPath);
            RelativePath = FullPath.Substring (root.Length).TrimStart ('/', '\\');
            AlbumPath = Path.GetDirectoryName (RelativePath).Trim ('/', '\\');
        }

        protected static bool IsValidFile (FileInfo fileInfo, HashSet<string> fileEndings)
        {
            return fileEndings.Contains (fileInfo.Extension.ToLower ());
        }

        public override string ToString ()
        {
            return string.Format ("[MediaFile: Name={0}, Extension={1}, AlbumPath={2}]", Name, Extension, AlbumPath);
        }

        public virtual void WriteAttributes (JsonWriter writer, MediaFile file, JsonSerializer serializer)
        {
            throw new NotImplementedException ();
        }

        public virtual void ReadAttributes (JObject jsonObject)
        {
            throw new NotImplementedException ();
        }
    }

    public sealed class MediaFileConverter : JsonConverter
    {
        public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        {
            MediaFile file = (MediaFile)value;
            writer.WriteStartObject ();
            writer.WritePropertyName ("FullPath");
            writer.WriteValue (file.FullPath);
            writer.WritePropertyName ("Root");
            writer.WriteValue (file.Root);
            writer.WritePropertyName ("Type");
            writer.WriteValue (value.GetType ().ToString ());
            file.WriteAttributes (writer, file, serializer);
            writer.WriteEndObject ();
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load (reader);
            string fullPath = (string)jsonObject.Property ("FullPath");
            string root = (string)jsonObject.Property ("Root");

            string type = (string)jsonObject.Property ("Type");
            MediaFile file;
            if (type == typeof(PictureFile).ToString ()) {
                file = new PictureFile (fullPath: fullPath, root: root);
            } else if (type == typeof(AudioFile).ToString ()) {
                file = new AudioFile (fullPath: fullPath, root: root);
            } else if (type == typeof(VideoFile).ToString ()) {
                file = new VideoFile (fullPath: fullPath, root: root);
            } else {
                throw new NotImplementedException ("Unknown type: " + objectType);
            }
            file.ReadAttributes (jsonObject: jsonObject);

            return file;
        }

        public override bool CanConvert (Type objectType)
        {
            return typeof(MediaFile).IsAssignableFrom (objectType);
        }
    }
}


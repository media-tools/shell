using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Pictures.Content;
using Shell.Common.Util;

namespace Shell.Pictures.Files
{
    [JsonConverter (typeof(MediaFileConverter))]
    public class MediaFile : ValueObject<MediaFile>
    {
        public string FullPath { get; private set; }

        public string Name { get; private set; }

        public string Extension { get; private set; }

        public string RelativePath { get; private set; }

        public string AlbumPath { get; private set; }

        public Media Media { get; private set; }

        public PictureShare Share { get; private set; }

        public MediaFile (string fullPath, PictureShare share)
            : this (fullPath: fullPath, hash: FileSystemUtilities.HashOfFile (path: fullPath), share: share)
        {
        }

        public MediaFile (string fullPath, HexString hash, PictureShare share)
        {
            Debug.Assert (fullPath.StartsWith (share.RootDirectory), "file path is not in root directory (FullName=" + fullPath + ",root=" + share.RootDirectory + ")");
            Share = share;
            FullPath = fullPath;
            Name = Path.GetFileName (fullPath);
            Extension = Path.GetExtension (fullPath);
            RelativePath = FullPath.Substring (Share.RootDirectory.Length).TrimStart ('/', '\\');
            AlbumPath = Path.GetDirectoryName (RelativePath).Trim ('/', '\\');

            Media media;
            if (share.GetMediaByHash (hash: hash, media: out media)) {
                Log.Debug ("cached: media");
            } else if (Picture.IsValidFile (fullPath: fullPath)) {
                media = new Picture (fullPath: fullPath);
                share.Add (media: media);
            } else if (Video.IsValidFile (fullPath: fullPath)) {
                media = new Video (fullPath: fullPath);
                share.Add (media: media);
            } else if (Audio.IsValidFile (fullPath: fullPath)) {
                media = new Audio (fullPath: fullPath);
                share.Add (media: media);
            } else {
                throw new ArgumentException ("[MediaFile] Unknown file: " + fullPath);
            }
            media.AddFile (mediaFile: this);
            Media = media;
        }

        public static bool IsValidFile (string fullPath)
        {
            return Picture.IsValidFile (fullPath: fullPath) || Audio.IsValidFile (fullPath: fullPath) || Video.IsValidFile (fullPath: fullPath);
        }

        public override string ToString ()
        {
            return string.Format ("[MediaFile: Name={0}, Extension={1}, AlbumPath={2}]", Name, Extension, AlbumPath);
        }

        protected override IEnumerable<object> Reflect()
        {
            return new object[] { RelativePath };
        }

        public static bool operator == (MediaFile c1, MediaFile c2)
        {
            return ValueObject<MediaFile>.Equality (c1, c2);
        }

        public static bool operator != (MediaFile c1, MediaFile c2)
        {
            return ValueObject<MediaFile>.Inequality (c1, c2);
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
            writer.WritePropertyName ("ShareConfigPath");
            writer.WriteValue (file.Share.ConfigPath);
            writer.WritePropertyName ("Hash");
            writer.WriteValue (file.Media.Hash);
            writer.WriteEndObject ();
        }

        public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load (reader);
            string fullPath = (string)jsonObject.Property ("FullPath");
            string shareConfigPath = (string)jsonObject.Property ("ShareConfigPath");
            HexString hash = new HexString { Hash = (string)jsonObject.Property ("Hash") };
            PictureShare share = PictureShare.CreateInstance (configPath: shareConfigPath);
            MediaFile file = new MediaFile (fullPath: fullPath, hash: hash, share: share);

            return file;
        }

        public override bool CanConvert (Type objectType)
        {
            return typeof(MediaFile).IsAssignableFrom (objectType);
        }
    }
}


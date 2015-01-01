using System;
using System.Drawing;
using Google.GData.Photos;
using Google.Picasa;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.GoogleSync.Photos
{
    public class WebPhoto
    {
        public AlbumCollection AlbumCollection { get; private set; }

        public WebAlbum Album { get; private set; }

        //private Photo InternalPhoto { get; set; }

        public string Title { get; private set; }

        public string Id { get; private set; }

        public Size Dimensions { get; private set; }

        public bool HasUniqueName { get; set; }

        public ulong TimestampUnix { get; private set; }

        public DateTime Timestamp { get { return DateTimeExtensions.MillisecondsTimeStampToDateTime (TimestampUnix); } }

        public string Filename { get { return HasUniqueName ? Title : Timestamp.ToString ("yyyyMMdd_HHmmss_") + Title; } }

        public WebPhoto (AlbumCollection albumCollection, WebAlbum album, Photo internalPhoto)
        {
            AlbumCollection = albumCollection;
            Album = album;
            HasUniqueName = true;
            Title = internalPhoto.Title;
            Id = internalPhoto.Id;
            TimestampUnix = internalPhoto.Timestamp;
            Dimensions = new Size (internalPhoto.Width, internalPhoto.Height);
        }
    }
}


using System;
using Google.GData.Photos;
using Google.Picasa;
using Shell.Common.IO;
using System.Drawing;

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

        public WebPhoto (AlbumCollection albumCollection, WebAlbum album, Photo internalPhoto)
        {
            AlbumCollection = albumCollection;
            Album = album;
            //InternalPhoto = internalPhoto;
            Update (internalPhoto);
        }

        private void Update (Photo internalPhoto)
        {
            Title = internalPhoto.Title;
            Id = internalPhoto.Id;
            Dimensions = new Size (internalPhoto.Width, internalPhoto.Height);
        }
    }
}


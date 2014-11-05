using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Tasks;
using Shell.Pictures.Content;

namespace Shell.Pictures.Files
{
    public sealed class MediaFileLibrary : Library
    {
        public MediaFileLibrary ()
        {
            ConfigName = "Pictures";
        }

        public bool IsUnknownMimeType (string mimeType)
        {
            return mimeType == "unknown/unknown" || mimeType == "application/binary" || mimeType == "application/octet-stream" || mimeType == "application/unknown";
        }

        public string GetMimeTypeByExternalCall (string fullPath)
        {
            string script = "file --brief --mime-type '" + fullPath + "'";

            string mimeType = null;
            Action<string> receiveOutput = line => {
                // example: image/jpeg
                mimeType = mimeType ?? line;
            };

            fs.Runtime.WriteAllText (path: "run3.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "run3.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true, verbose: false);

            return mimeType ?? "application/octet-stream";
        }

        private Dictionary<string, string> mimeTypeToExtension = new Dictionary<string, string> ();
        private Dictionary<string, string> extensionToMimeType = new Dictionary<string, string> ();

        private void createMimeTypeDictionary (Dictionary<string, string> _mimeTypeToExtension)
        {
            foreach (var entry in _mimeTypeToExtension) {
                mimeTypeToExtension [entry.Key] = entry.Value;
                extensionToMimeType [entry.Value] = entry.Key;
            }
        }

        private void createMimeTypeDictionary ()
        {
            createMimeTypeDictionary (Picture.MIME_TYPES);
            createMimeTypeDictionary (Video.MIME_TYPES);
            createMimeTypeDictionary (Audio.MIME_TYPES);
            createMimeTypeDictionary (Document.MIME_TYPES);
        }

        public string GetMimeTypeByExtension (string fullPath)
        {
            if (extensionToMimeType.Count == 0) {
                createMimeTypeDictionary ();
            }
            string extension = Path.GetExtension (fullPath);
            return extensionToMimeType.ContainsKey (extension) ? extensionToMimeType [extension] : null;
        }

        public string GetExtensionByMimeType (string mimeType)
        {
            if (extensionToMimeType.Count == 0) {
                createMimeTypeDictionary ();
            }
            return mimeTypeToExtension.ContainsKey (mimeType) ? mimeTypeToExtension [mimeType] : null;
        }
    }
}


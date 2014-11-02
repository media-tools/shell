using System;
using Shell.Common.Tasks;

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

        public string GetMimeType (string fullPath)
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
    }
}


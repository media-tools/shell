using System;
using Shell.Common.Tasks;

namespace Shell.Pictures
{
    public class PictureFindSharesTask : ScriptTask, MainScriptTask
    {
        public PictureFindSharesTask ()
        {
            Name = "PictureFindShares";
            ConfigName = "Pictures";
            Description = "Create an index of all picture directories";
            Options = new string[] { "picture-find-shares", "p-fs" };
        }

        protected override void InternalRun (string[] args)
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: false);
            shares.Print ();
        }
    }
}


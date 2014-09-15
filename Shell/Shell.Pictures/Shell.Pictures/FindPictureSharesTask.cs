using System;
using Shell.Common.Tasks;

namespace Shell.Pictures
{
    public class FindPictureSharesTask : ScriptTask, MainScriptTask
    {
        public FindPictureSharesTask ()
        {
            Name = "FindMediaShares";
            ConfigName = "Pictures";
            Description = "Create an index of all picture directories";
            Options = new string[] { "picture-find-shares", "p-fs" };
        }

        protected override void InternalRun (string[] args)
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: fs);
            shares.Initialize (cached: false);
            shares.Print ();
        }
    }
}


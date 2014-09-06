using System;
using Shell.Common.Tasks;
using Shell.Common;

namespace Shell.HolePunching
{
    public class FuckTask : Task, MainTask
    {
        public FuckTask ()
        {
            Name = "HolePunchingServer";
            Description = "Run the hole punching server";
            Options = new string[] { "hole-punching-server", "hp-server" };
            ConfigName = "HolePunching";
            ParameterSyntax = "";
        }

        protected override void InternalRun (string[] args)
        {
            int port = NetworkUtils.CurrentPort (0);
            int backlog = 10;

            Console.ReadLine ();
        }
    }
}


using System;
using Shell.Common.Tasks;
using Shell.Common;

namespace Shell.HolePunching
{
    public class ServerTask : Task, MainTask
    {
        public ServerTask ()
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

            HolePunchingServer hps = new HolePunchingServer (port, backlog);
            hps.BeginServer ();

            Console.ReadLine ();
        }
    }
}


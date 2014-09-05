using System;
using Shell.Compatibility;
using Mono.Unix.Native;

namespace Shell.Compatibility.Linux
{
    public class LinuxSystemHelper : SystemHelper
    {
        public override uint GetUid ()
        {
            return Syscall.getuid();
        }
    }
}


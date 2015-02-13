using System;
using Shell.Common.Tasks;

namespace Shell.Common.Hooks
{
    public abstract class Hook
    {
        public Hook ()
        {
        }

        public abstract void HookBeforeTask (ScriptTask task);

        public abstract void HookAfterTask (ScriptTask task);
    }
}


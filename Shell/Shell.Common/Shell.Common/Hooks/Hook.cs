using System;
using Shell.Common.Tasks;

namespace Shell.Common.Hooks
{
    public abstract class Hook
    {
        public Hook ()
        {
        }

        public abstract void HookBeforeTask (Task task);

        public abstract void HookAfterTask (Task task);
    }
}


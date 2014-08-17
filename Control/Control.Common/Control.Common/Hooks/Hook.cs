using System;
using Control.Common.Tasks;

namespace Control.Common.Hooks
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


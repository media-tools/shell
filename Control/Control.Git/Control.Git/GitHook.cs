using System;
using Control.Common;
using Control.Common.Tasks;
using Control.Common.Hooks;

namespace Control.Git
{
    public class GitHook : Hook
    {
        public override void HookBeforeTask (Task task)
        {
            if (!(task is ConfigGitTask || task is ConfigCommitTask)) {
                GitLibrary.Commit ();
            }
        }

        public override void HookAfterTask (Task task)
        {
        }
    }
}


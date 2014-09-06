using System;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.Hooks;

namespace Shell.Git
{
    public class GitHook : Hook
    {
        public override void HookBeforeTask (ScriptTask task)
        {
            if (!(task is ConfigGitTask || task is ConfigCommitTask)) {
                GitLibrary.Commit ();
            }
        }

        public override void HookAfterTask (ScriptTask task)
        {
        }
    }
}


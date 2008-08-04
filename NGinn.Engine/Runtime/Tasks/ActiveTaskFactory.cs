using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime.Tasks
{
    internal class ActiveTaskFactory : IActiveTaskFactory
    {
        public IActiveTask CreateActiveTask(Task tsk)
        {
            /*
            ActiveTransition at;
            if (tsk is ManualTask)
            {
                at = new ManualTaskActive((ManualTask)tsk);
            }
            else if (tsk is SubprocessTask)
            {
                at = new SubprocessTaskActive((SubprocessTask)tsk);
            }
            else if (tsk is EmptyTask)
            {
                at = new EmptyTaskActive((EmptyTask)tsk);
            }
            else if (tsk is ScriptTask)
            {
                at = new ScriptTaskActive((ScriptTask)tsk);
            }
            else if (tsk is TimerTask)
            {
                at = new TimerTaskActive((TimerTask)tsk);
            }
            else throw new Exception();
            return at;
            */
            return new EmptyTaskActive(tsk);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;

namespace NGinn.Engine
{
    internal class ActiveTransitionFactory
    {
        public ActiveTransition CreateTransition(ProcessInstance pi, Task tsk)
        {
            ActiveTransition at;
            if (tsk is ManualTask)
            {
                at = new ManualTaskActive((ManualTask)tsk, pi);
            }
            else if (tsk is SubprocessTask)
            {
                at = new SubprocessTaskActive((SubprocessTask)tsk, pi);
            }
            else throw new Exception();
            at.ProcessInstanceId = pi.InstanceId;
            at.Activate();
            return at;
        }
    }
}

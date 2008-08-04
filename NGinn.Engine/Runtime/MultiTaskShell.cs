using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// Multi-instance task shell
    /// </summary>
    class MultiTaskShell : TaskShell
    {
        public MultiTaskShell(ProcessInstance pi, Task tsk)
        {
            this.TaskId = tsk.Id;
            this.ProcessInstanceId = pi.InstanceId;
            SetProcessInstance(pi);
        }

        public TaskShell GetChildTransition(string correlationId)
        {
            return null;
        }

    }
}

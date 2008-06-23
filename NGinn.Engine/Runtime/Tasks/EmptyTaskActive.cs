using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Empty (routing-only) task.
    /// </summary>
    [Serializable]
    public class EmptyTaskActive : ActiveTransition
    {
        public EmptyTaskActive(EmptyTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {

        }

        protected override void DoCancelTask()
        {
        }

        protected override void DoExecuteTask()
        {   
        }

        protected override void DoInitiateTask()
        {   
        }
    }

}

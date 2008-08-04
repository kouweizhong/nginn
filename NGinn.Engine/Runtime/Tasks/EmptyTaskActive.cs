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
    public class EmptyTaskActive : ActiveTaskBase
    {
        public EmptyTaskActive(Task tsk)
            : base(tsk)
        {

        }

        public override void CancelTask()
        {
            
        }

        public override void InitiateTask()
        {
        }

        public override void ExecuteTask()
        {
        }
    }

}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Receive message task.
    /// It waits for a message.
    /// </summary>
    [Serializable]
    public class ReceiveMessageTaskActive : ActiveTaskBase
    {
        public ReceiveMessageTaskActive(Task tsk)
            : base(tsk)
        {
        }

        protected override void DoInitiateTask()
        {
            throw new NotImplementedException();
        }

        public override void CancelTask()
        {
            throw new NotImplementedException();
        }
    }
}

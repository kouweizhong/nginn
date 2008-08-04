using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Email notification task
    /// </summary>
    [Serializable]
    public class NotificationTaskActive : ActiveTransition
    {
        public NotificationTaskActive(NotificationTask tsk)
            : base(tsk)
        {

        }

        protected override void DoCancelTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoInitiateTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoExecuteTask()
        {
            throw new NotImplementedException();
        }
    }
}

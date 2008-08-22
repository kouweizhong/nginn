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
    public class NotificationTaskActive : ActiveTaskBase
    {
        public NotificationTaskActive(NotificationTask tsk)
            : base(tsk)
        {

        }

        public override void CancelTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoInitiateTask()
        {
            throw new NotImplementedException();
        }

        
    }
}

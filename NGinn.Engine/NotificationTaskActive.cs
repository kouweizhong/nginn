using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine
{
    [Serializable]
    public class NotificationTaskActive : ActiveTransition
    {
        public NotificationTaskActive(NotificationTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {

        }

        public override void InitiateTask()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}

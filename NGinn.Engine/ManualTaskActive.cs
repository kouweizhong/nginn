using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine
{
    [Serializable]
    class ManualTaskActive : ActiveTransition
    {
        public ManualTaskActive(ManualTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {
         
        }

        public override void Activate()
        {
            base.Activate();
        }

        public override void Passivate()
        {
            base.Passivate();
        }

        /// <summary>
        /// Initiate manual task
        /// </summary>
        public override void InitiateTask()
        {
            this.CorrelationId = this.ProcessInstanceId;
        }
    }
}

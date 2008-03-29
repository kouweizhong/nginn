using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Engine.Services;
using NLog;

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
            WorkItem wi = new WorkItem();
            wi.ProcessInstanceId = this.ProcessInstanceId;
            wi.TaskId = this.TaskId;
            wi.CorrelationId = this.CorrelationId;
            wi.Title = this.ProcessTask.Id;
            this._processInstance.Environment.WorklistService.CreateWorkItem(wi);
            log.Info("Created work item");
        }

        public override void CancelTask()
        {
            this._processInstance.Environment.WorklistService.CancelWorkItem(this.CorrelationId);
        }
    }
}

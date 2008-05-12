using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Engine.Services;
using NLog;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.Worklist;

namespace NGinn.Engine.Runtime.Tasks
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

        public ManualTask TheTask
        {
            get { return (ManualTask)ProcessTask; }
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
            wi.Title = TheTask.Id;
            this._processInstance.Environment.WorklistService.CreateWorkItem(wi);
            log.Info("Created work item");
        }

        public override void CancelTask()
        {
            this._processInstance.Environment.WorklistService.CancelWorkItem(this.CorrelationId);
        }

        public override void TaskCompleted()
        {
            base.TaskCompleted();
            this._processInstance.Environment.WorklistService.WorkItemCompleted(CorrelationId);
        }
    }
}

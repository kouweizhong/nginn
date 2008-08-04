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
        public ManualTaskActive(ManualTask tsk)
            : base(tsk)
        {
         
        }

        public ManualTask TheTask
        {
            get { return (ManualTask)ProcessTask; }
        }


        protected override void DoExecuteTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoInitiateTask()
        {
            WorkItem wi = new WorkItem();
            wi.ProcessInstanceId = this.ProcessInstanceId;
            wi.TaskId = this.TaskId;
            wi.CorrelationId = this.CorrelationId;
            wi.Title = TheTask.Id;
            this._processInstance.Environment.WorklistService.CreateWorkItem(wi);
            log.Info("Created work item");
        }

        protected override void DoCancelTask()
        {
            this._processInstance.Environment.WorklistService.CancelWorkItem(this.CorrelationId);
        }

        /// <summary>
        /// Manual task completion
        /// </summary>
        /// <param name="tci"></param>
        protected override void OnTaskCompleted(TaskCompletionInfo tci)
        {
            base.OnTaskCompleted(tci);
            this._processInstance.Environment.WorklistService.WorkItemCompleted(this.CorrelationId);
        }
    }
}

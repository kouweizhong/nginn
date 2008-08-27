using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Engine.Services;
using NLog;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.Worklist;
using NGinn.Lib.Data;

namespace NGinn.Engine.Runtime.Tasks
{
    [Serializable]
    class ManualTaskActive : ActiveTaskBase
    {
        
        public ManualTaskActive(ManualTask tsk)
            : base(tsk)
        {
         
        }

        

        private string _title;

        [TaskParameter(IsInput=true, Required=false, DynamicAllowed=true)]
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }
        private string _assigneeId;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string AssigneeId
        {
            get { return _assigneeId; }
            set { _assigneeId = value; }
        }
        private string _assigneeGroup;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string AssigneeGroup
        {
            get { return _assigneeGroup; }
            set { _assigneeGroup = value; }
        }
        private string _description;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }


        protected override void DoInitiateTask()
        {
            WorkItem wi = new WorkItem();
            wi.ProcessInstanceId = Context.ProcessInstanceId;
            wi.TaskId = Context.TaskDefinition.Id;
            wi.CorrelationId = this.CorrelationId;
            wi.Title = Title;
            wi.Description = Description;
            wi.AssigneeGroupId = AssigneeGroup;
            wi.AssigneeId = AssigneeId;
            wi.TaskData = new DataObject(VariablesContainer);
            wi.SharedId = Context.SharedId;

            Context.Environment.WorklistService.CreateWorkItem(wi);
            log.Info("Created work item");
        }

        public override void CancelTask()
        {
            log.Info("Cancelling manual task {0}", CorrelationId);
            Context.Environment.WorklistService.CancelWorkItem(this.CorrelationId);
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime.Tasks
{
    [Serializable]
    public class ReceiveMessageTaskEvent : InternalTransitionEvent
    {
        public string MessageCorrelationId;
        public DataObject MessageData;
    }
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

        private string _messageCorrelationId = null;

        [TaskParameter(IsInput=true, Required=false, DynamicAllowed=true)]
        public string MessageCorrelationId
        {
            get { return _messageCorrelationId; }
            set { _messageCorrelationId = value; }
        }

        

        protected override void DoInitiateTask()
        {
            if (MessageCorrelationId == null) //assume there's only one instance of this receive message task 
            {
                MessageCorrelationId = string.Format("{0}.{1}", Context.ProcessInstanceId, Context.TaskDefinition.Id);
            }
            Context.EnvironmentContext.CorrelationIdResolver.RegisterMapping(this.MessageCorrelationId, this.CorrelationId);
            log.Info("Task {0} is waiting for message with message correlationID={1}", this.CorrelationId, this.MessageCorrelationId);
        }

        public override void CancelTask()
        {
            log.Info("Task {0} is cancelling. Removing mapping for message with message correlationID={1}", this.CorrelationId, this.MessageCorrelationId);
            Context.EnvironmentContext.CorrelationIdResolver.RemoveMapping(this.MessageCorrelationId, this.CorrelationId);
        }

        public override bool HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            if (!(ite is ReceiveMessageTaskEvent)) throw new Exception("Invalid event type");
            ReceiveMessageTaskEvent ev = (ReceiveMessageTaskEvent)ite;
            //ok, retrieve message data and complete the task...
            if (ev.MessageCorrelationId != null && !string.Equals(ev.MessageCorrelationId, this.MessageCorrelationId))
                throw new ApplicationException(string.Format("Task {0}: invalid message correlation id: {1} (expected: {2})", CorrelationId, ev.MessageCorrelationId, this.MessageCorrelationId));
            //TODO: retrieve and validate message data
            ev.MessageData.Validate(Context.TaskDefinition.GetTaskOutputDataSchema());
            foreach (string fldName in ev.MessageData.FieldNames)
            {
                VariablesContainer[fldName] = ev.MessageData[fldName];
            }
            Context.EnvironmentContext.CorrelationIdResolver.RemoveMapping(this.MessageCorrelationId, this.CorrelationId);
            OnTaskCompleted();
            return true;
        }
    }
}

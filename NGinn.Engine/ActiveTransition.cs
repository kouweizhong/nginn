using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine
{
    public enum TransitionStatus
    {
        Initiated, //task created & offered
        Completed, //task finished
        Error, //task did not complete due to error
    }

    /// <summary>
    /// Represents an 'active' counterpart of workflow transition (Task). Task is a definition of an activity, and
    /// ActiveTransition subclasses define instances of particular task with logic for implementing them.
    /// </summary>
    [Serializable]
    public abstract class ActiveTransition
    {
        public string ProcessInstanceId;
        public string TaskId;
        public IList<string> Tokens;
        public TransitionStatus Status;
        public string CorrelationId;
        [NonSerialized]
        protected ProcessInstance _processInstance;

        public ActiveTransition(Task tsk, ProcessInstance pi)
        {
            this.Status = TransitionStatus.Initiated;
            this.TaskId = tsk.Id;
            this._processInstance = pi;
            this.ProcessInstanceId = pi.InstanceId;
        }

        public virtual void SetProcessInstance(ProcessInstance pi)
        {
            if (this.ProcessInstanceId != pi.InstanceId) throw new Exception("Invalid process instance ID");
            this._processInstance = pi;
        }

        /// <summary>
        /// Called after deserialization
        /// </summary>
        public virtual void Activate()
        {
            if (_processInstance == null) throw new Exception("Process instance not set (call SetProcessInstance before activating)");
        }

        /// <summary>
        /// Called before serialization
        /// </summary>
        public virtual void Passivate()
        {
            _processInstance = null;
        }

        /// <summary>
        /// Initiate task (start the transition).
        /// If the transition is immediate, this operation will execute the task.
        /// If the transition is not immediate, this will initiate the transition.
        /// </summary>
        public abstract void InitiateTask();
        
    }
}

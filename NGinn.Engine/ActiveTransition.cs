using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;

namespace NGinn.Engine
{
    public enum TransitionStatus
    {
        ENABLED,    //transition task created & offered (also for deferred choice to be selected)
        STARTED,    //transition task started (deferred choice alternative has been selected)
        COMPLETED,  //task finished
        CANCELLED,  //task cancelled (other transition sharing the same token fired)
        ERROR,      //task did not complete due to error
    }

    /// <summary>
    /// Represents an 'active' counterpart of workflow transition (Task). Task is a definition of an activity, and
    /// ActiveTransition subclasses define instances of particular task with logic for implementing them.
    /// </summary>
    [Serializable]
    public abstract class ActiveTransition
    {
        /// <summary>Process instance Id</summary>
        public string ProcessInstanceId;
        /// <summary>Correlation id. Warning: it should be unique in scope of a single process. 
        /// CorrelationId should be present after task has been initiated.</summary>
        private string _correlationId;
        /// <summary>Id of task in a process</summary>
        public string TaskId;
        public IList<string> Tokens = new List<string>();
        public TransitionStatus Status;
        /// <summary>If active transitions share some tokens, they will have the same SharedId. If one of 
        /// shared transitions completes, it will cancell all other transitions with the same SharedId
        /// </summary>
        public string SharedId;
        [NonSerialized]
        protected ProcessInstance _processInstance;
        [NonSerialized]
        protected Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        private bool _activated = false;

        public ActiveTransition(Task tsk, ProcessInstance pi)
        {
            this.Status = TransitionStatus.ENABLED;
            this.TaskId = tsk.Id;
            this._processInstance = pi;
            this.ProcessInstanceId = pi.InstanceId;
        }

        public virtual void SetProcessInstance(ProcessInstance pi)
        {
            if (this.ProcessInstanceId != pi.InstanceId) throw new Exception("Invalid process instance ID");
            this._processInstance = pi;
        }

        public string CorrelationId
        {
            get { return _correlationId; }
            set { if (_activated) throw new Exception("Cannot modify after activation"); _correlationId = value; }
        }

        /// <summary>
        /// Called after deserialization
        /// </summary>
        public virtual void Activate()
        {
            if (_processInstance == null) throw new Exception("Process instance not set (call SetProcessInstance before activating)");
            _activated = true;
        }

        /// <summary>
        /// Called before serialization
        /// </summary>
        public virtual void Passivate()
        {
            _processInstance = null;
            _activated = false;
        }

        protected Task ProcessTask
        {
            get { return _processInstance.Definition.GetTask(TaskId); }
        }

        public bool CanInitiateTask()
        {
            Dictionary<string, Token> toks = new Dictionary<string, Token>();
            foreach (string tid in Tokens)
            {
                
            }
            Task t = ProcessTask;
            if (t.JoinType == JoinType.AND)
            {
                foreach (Place pl in t.NodesIn)
                {
                    
                }
            }
            else if (t.JoinType == JoinType.XOR)
            {
            }
            else if (t.JoinType == JoinType.OR)
            {
            }
            return false;
            
        }

        /// <summary>
        /// Initiate task (start the transition).
        /// If the transition is immediate, this operation will execute the task.
        /// If the transition is not immediate, this will initiate the transition.
        /// Subclasses should override this function, but should always call base.InitiateTask()
        /// </summary>
        public virtual void InitiateTask()
        {
            if (this.Tokens.Count == 0) throw new Exception("No input tokens");

        }

        public virtual bool IsImmediate
        {
            get
            {
                return this.ProcessTask.IsImmediate;
            }
        }

        public virtual void ExecuteTask()
        {   
        }

        public virtual void CancelTask()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Engine;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;

namespace NGinn.Engine.Runtime
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
    /// Task shell wraps active task instance providing common interface between task instance and
    /// process instance
    /// </summary>
    [Serializable]
    class TaskShell : IActiveTaskContext
    {
        private string _correlationId;
        private string _processInstanceId;
        private object _activeTask;
        private string _sharedId;
        private TransitionStatus _status;
        private string _taskId;
        [NonSerialized]
        private Task _taskDefinition;

        public TaskShell(ProcessInstance pi, Task tsk)
        {
            this.TaskId = tsk.Id;
            _taskDefinition = tsk;
            _processInstance = pi;
            _processInstanceId = pi.InstanceId;
            Status = TransitionStatus.ENABLED;
        }

        protected TaskShell()
        {
        }

        public string TaskId
        {
            get { return _taskId; }
            set { _taskId = value; }
        }

        public string SharedId
        {
            get { return _sharedId; }
            set { _sharedId = value; }
        }

        public string ProcessInstanceId
        {
            get { return _processInstanceId; }
            set { _processInstanceId = value; }
        }

        public TransitionStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public IActiveTask ActiveTask
        {
            get { return _activeTask as IActiveTask; }
            set { _activeTask = value; }
        }

        public string CorrelationId
        {
            get { return _correlationId; }
            set { _correlationId = value; }
        }

        public IList<string> Tokens = new List<string>();

        [NonSerialized]
        protected ProcessInstance _processInstance;
        protected ITransitionCallback _parentCallback;
        [NonSerialized]
        protected Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        private bool _activated = false;

        public bool IsImmediate
        {
            get { return ActiveTask.IsImmediate; }
        }

        public ITransitionCallback ParentCallback
        {
            get { return _parentCallback; }
            set { _parentCallback = value; }
        }

        public void CancelTask()
        {
            ActiveTask.CancelTask();
            this.Status = TransitionStatus.CANCELLED;
        }

        public void InitiateTask()
        {
            ActiveTask.InitiateTask();
            this.Status = TransitionStatus.ENABLED;
        }

        public void ExecuteTask()
        {
            ActiveTask.ExecuteTask();
            this.Status = TransitionStatus.COMPLETED;
            ParentCallback.TransitionCompleted(ActiveTask.CorrelationId);
        }

        public void SetProcessInstance(ProcessInstance pi)
        {
            if (pi.InstanceId != this.ProcessInstanceId) throw new Exception();
            _processInstance = pi;
            if (_parentCallback == null) _parentCallback = pi as ITransitionCallback;
        }

        public virtual void Activate()
        {
            _activated = true;
        }

        public virtual void Passivate()
        {
            _processInstance = null;
            _activated = false;
        }

        public virtual void TransferInputDataToTask(IDataObject dob)
        {
            bool createNewTask
            if (_activeTask == null)
            {
                IActiveTask aTask = ParentProcess.Environment.ActiveTaskFactory.CreateActiveTask(TaskDefinition);
                InitializeActiveTask(aTask);
            
            }

        }

        public virtual void ReceiveOutputDataFromTask(IDataObject target)
        {

        }

        public IDataObject GetTaskData()
        {
            return ActiveTask.GetTaskData();
        }

        public virtual void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            ActiveTask.HandleInternalTransitionEvent(ite);
        }

        protected void ActivationRequired(bool activated)
        {
            if (_activated != activated) throw new Exception();
        }

        public virtual void NotifyTransitionSelected()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED &&
                this.Status != TransitionStatus.STARTED)
                throw new Exception("Status invalid");
            ActiveTask.NotifyTransitionSelected();
            this.Status = TransitionStatus.STARTED;
            _parentCallback.TransitionStarted(this.CorrelationId);
        }

        public virtual void NotifyTaskCompleted(TaskCompletionInfo tci)
        {
        }

            







        #region IActiveTaskContext Members


        public NGinn.Lib.Schema.Task TaskDefinition
        {
            get { throw new NotImplementedException(); }
        }

        public ProcessInstance ParentProcess
        {
            get { return _processInstance; }
        }

        public void TransitionStarted(string correlationId)
        {
            throw new NotImplementedException();
        }

        public void TransitionCompleted(string correlationId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

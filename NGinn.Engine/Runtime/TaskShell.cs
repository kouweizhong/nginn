using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Engine;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;
using ScriptNET;

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
        [NonSerialized]
        private IDataObject _taskOutputData = null;

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

        private List<string> _tokens = new List<string>();

        /// <summary>
        /// List of task's input tokens
        /// </summary>
        public IList<string> Tokens
        {
            get { return _tokens; }
        }


        [NonSerialized]
        protected ProcessInstance _processInstance;
        protected ITransitionCallback _parentCallback;
        [NonSerialized]
        protected Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        protected bool _activated = false;

        public bool IsImmediate
        {
            get { return TaskDefinition.IsImmediate; }
        }

        public ITransitionCallback ParentCallback
        {
            get { return _parentCallback; }
            set { _parentCallback = value; }
        }

        public virtual void CancelTask()
        {
            ActiveTask.CancelTask();
            this.Status = TransitionStatus.CANCELLED;
        }

        /// <summary>
        /// Execute task, transferring data in and out of the task
        /// </summary>
        /// <param name="sourceData">Source for task data</param>
        /// <param name="outputTargetData">Target for task data - usually the same as source</param>
        /*public virtual void ExecuteTask(IDataObject sourceData, IDataObject outputTargetData)
        {
            IActiveTask tsk = CreateActiveTask();
            tsk.Activate();
            if (this.IsImmediate) throw new Exception("Cannot execute - non immediate task");
            DataObject taskInputData = PrepareTaskInputData(sourceData);
            _taskOutputData = null;
            tsk.InitiateTask(taskInputData);
            if (_taskOutputData == null) throw new Exception("Output data is null - immediate task did not call back on completion");
            Status = TransitionStatus.COMPLETED;
            TransferTaskOutputDataToParent(_taskOutputData, outputTargetData);
        }
        */

        /// <summary>
        /// Task output data - valid only after task has been completed.
        /// Warning - this member is valid only during 
        /// </summary>
        public IDataObject TaskOutputData
        {
            get { return _taskOutputData; }
        }

        public virtual void InitiateTask(IDataObject sourceData)
        {
            if (ActiveTask != null) throw new Exception("Active task already created");
            IActiveTask tsk = CreateActiveTask();
            tsk.Activate();
            DataObject taskInputData = PrepareTaskInputData(sourceData);
            _taskOutputData = null;
            Status = TransitionStatus.ENABLED;
            this.ActiveTask = tsk;
            tsk.InitiateTask(taskInputData);
            if (tsk.IsImmediate)
            {
                if (Status != TransitionStatus.COMPLETED) throw new Exception("Immediate task did not complete");
                if (_taskOutputData == null) throw new Exception("Task output data missing");
                this.ActiveTask = null; 
            }
            else
            {
            }
        }

        /// <summary>
        /// Create new active task instance
        /// </summary>
        /// <returns></returns>
        protected IActiveTask CreateActiveTask()
        {
            IActiveTask tsk = this.ParentProcess.Environment.ActiveTaskFactory.CreateActiveTask(TaskDefinition);
            tsk.CorrelationId = this.CorrelationId;
            tsk.SetContext(this);
            return tsk;
        }
        
        /// <summary>
        /// Create task input data (execute bindings on source data)
        /// </summary>
        /// <param name="sourceData"></param>
        /// <returns></returns>
        protected DataObject PrepareTaskInputData(IDataObject sourceData)
        {
            IScriptContext ctx = CreateTaskScriptContext(sourceData);
            DataObject taskInput = new DataObject();
            DataBinding.ExecuteDataBinding(taskInput, TaskDefinition.InputBindings, ctx);
            return taskInput;
        }

        protected void TransferTaskOutputDataToParent(IDataObject taskOutputData, IDataObject targetObj)
        {
            IScriptContext ctx = CreateTaskScriptContext(taskOutputData);
            DataBinding.ExecuteDataBinding(targetObj, TaskDefinition.OutputBindings, ctx);
        }

        public virtual void TransferTaskOutputDataToParent(IDataObject target)
        {
            if (TaskOutputData == null) throw new Exception("Task output data is null");
            TransferTaskOutputDataToParent(this.TaskOutputData, target);
        }

        protected IScriptContext CreateTaskScriptContext(IDataObject variables)
        {
            IScriptContext ctx = new ScriptContext();
            foreach (string fn in variables.FieldNames)
            {
                ctx.SetItem(fn, ContextItem.Variable, variables[fn]);
            }
            return ctx;
        }

        public void SetProcessInstance(ProcessInstance pi)
        {
            if (pi.InstanceId != this.ProcessInstanceId) throw new Exception();
            _processInstance = pi;
            if (_parentCallback == null) _parentCallback = pi as ITransitionCallback;
        }

        public virtual void Activate()
        {
            if (_activated) throw new Exception("Already activated");
            if (_processInstance == null || _parentCallback == null)
                throw new Exception("Set process instance and parent callback before activation");
            _taskDefinition = ParentProcess.Definition.GetTask(this.TaskId);
            if (ActiveTask != null)
            {
                ActiveTask.SetContext(this);
                ActiveTask.Activate();
            }
            _activated = true;
        }

        public virtual void Passivate()
        {
            if (ActiveTask != null)
            {
                ActiveTask.Passivate();
            }
            _processInstance = null;
            _taskDefinition = null;
            _parentCallback = null;
            _activated = false;
        }

        
        public IDataObject GetTaskData()
        {
            ActivationRequired(true);
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

        public Task TaskDefinition
        {
            get { return _taskDefinition; }
        }

        public ProcessInstance ParentProcess
        {
            get { return _processInstance; }
        }

        void IActiveTaskContext.TransitionStarted(string correlationId)
        {
            throw new NotImplementedException();
        }

        void IActiveTaskContext.TransitionCompleted(string correlationId, DataObject taskOutputData)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Engine;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;
using System.Diagnostics;
using System.Collections;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime
{
    public enum TransitionStatus
    {
        ENABLED,    ///transition task created & offered (also for deferred choice to be selected)
        STARTED,    ///transition task started (deferred choice alternative has been selected)
        COMPLETED,  ///task finished
        CANCELLED,  ///task cancelled (other transition sharing the same token fired)
        ERROR,      ///task did not complete due to error
    }
    /// <summary>
    /// Task shell wraps active task instance providing common interface between task instance and
    /// process instance
    /// Restoring task state: task state is restored in two phases. First, TaskShell's fields
    /// are initialized and active task's state is remembered. The task will be restored
    /// on activation. Cannot do it earlier because TaskShell's members are not initialized.
    /// </summary>
    [Serializable]
    class TaskShell : IActiveTaskContext, INGinnPersistent
    {
        private string _correlationId;
        private object _activeTask;
        private string _sharedId;
        private TransitionStatus _status;
        private string _taskId;
        
        [NonSerialized]
        private Task _taskDefinition;
        [NonSerialized]
        private IDataObject _taskOutputData = null;
        [NonSerialized]
        private INGEnvironmentContext _envCtx;
        /// <summary>restored task state - before activation</summary>
        [NonSerialized]
        private DataObject _taskState;
        [NonSerialized]
        ProcessInstance _parentProces;
        public TaskShell()
        {
            Status = TransitionStatus.ENABLED;
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

        public INGEnvironmentContext EnvironmentContext
        {
            get { return _envCtx; }
            set { _envCtx = value; }
        }

        public string ProcessInstanceId
        {
            get { return _parentProces.InstanceId; }
        }

        public ProcessInstance ParentProcess
        {
            get { return _parentProces; }
        }

        public void SetProcessInstance(ProcessInstance pi)
        {
            _parentProces = pi;
            if (_parentCallback == null) _parentCallback = pi;
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

        private List<string> _allocatedPlaces;

        public IList<string> AllocatedPlaces
        {
            get { return _allocatedPlaces; }
        }

        public void SetAllocatedPlaces(IList<string> lst)
        {
            _allocatedPlaces = new List<string>(lst);
        }


        [NonSerialized]
        protected IProcessTransitionCallback _parentCallback;
        [NonSerialized]
        protected Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        protected bool _activated = false;

        public IProcessTransitionCallback ParentCallback
        {
            get { return _parentCallback; }
            set { _parentCallback = value; }
        }

        /// <summary>
        /// Cancel enabled or started transition
        /// </summary>
        public virtual void CancelTask()
        {
            RequireActivation(true);
            ActiveTask.CancelTask();
        }

        /// <summary>
        /// Make sure task shell is activated or not
        /// </summary>
        /// <param name="activated"></param>
        protected void RequireActivation(bool activated)
        {
            if (activated != _activated) throw new Exception(activated ? "Error: activation required" : "Error: task shell should be passivated");
        }
        

        /// <summary>
        /// Task output data - valid only after task has been completed.
        /// </summary>
        public IDataObject TaskOutputData
        {
            get { return _taskOutputData; }
        }

        /// <summary>
        /// Enable the transition.
        /// </summary>
        /// <param name="sourceData">Process instance data</param>
        public virtual void InitiateTask(IDataObject sourceData)
        {
            RequireActivation(true);
            if (ActiveTask != null) throw new Exception("Active task already created");
            
            IActiveTask tsk = CreateActiveTask();
            tsk.Activate();
            DataObject taskInputData = PrepareTaskInputData(sourceData);
            _taskOutputData = null;
            this.ActiveTask = tsk;
            tsk.InitiateTask(taskInputData);
        }

        /// <summary>
        /// Create new active task instance
        /// </summary>
        /// <returns></returns>
        protected IActiveTask CreateActiveTask()
        {
            IActiveTask tsk =  this.EnvironmentContext.ActiveTaskFactory.CreateActiveTask(TaskDefinition);
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
            ITaskScript scr = CreateTaskScriptContext(sourceData);
            DataObject taskInput = new DataObject();
            foreach (VariableBinding vb in TaskDefinition.InputBindings)
            {
                if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
                {
                    taskInput[vb.VariableName] = sourceData[vb.BindingExpression];
                }
                else if (vb.BindingType == VariableBinding.VarBindingType.Expr)
                {
                    taskInput[vb.VariableName] = scr.EvalInputVariableBinding(vb.VariableName);
                }
                else throw new Exception("Binding type not supported");
            }
            return taskInput;
        }

        protected void TransferTaskOutputDataToParent(IDataObject taskOutputData, IDataObject targetObj)
        {
            ITaskScript scr = CreateTaskScriptContext(taskOutputData);
            
            foreach (VariableBinding vb in TaskDefinition.OutputBindings)
            {
                if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
                {
                    targetObj[vb.VariableName] = taskOutputData[vb.BindingExpression];
                }
                else if (vb.BindingType == VariableBinding.VarBindingType.Expr)
                {
                    targetObj[vb.VariableName] = scr.EvalOutputVariableBinding(vb.VariableName);
                }
                else throw new Exception("Binding type not supported");
            }
        }

        public virtual void TransferTaskOutputDataToParent(IDataObject target)
        {
            RequireActivation(true);
            if (TaskOutputData == null) throw new Exception("Task output data is null");
            TransferTaskOutputDataToParent(this.TaskOutputData, target);
        }

        protected ITaskScript CreateTaskScriptContext(IDataObject variables)
        {
            ITaskScript scr = this.EnvironmentContext.ScriptManager.GetTaskScript(ParentProcess.Definition, TaskId);
            scr.SourceData = variables;
            return scr;
        }

        public virtual void Activate()
        {
            lock (this)
            {
                if (_activated) throw new Exception("Already activated");
                if (_parentCallback == null)
                    throw new Exception("Set process parent callback before activation");
                if (_envCtx == null) throw new Exception("EnvironmentContext not set");
                if (_parentProces == null) throw new Exception("Process instance not set");
                if (TaskId == null) throw new Exception("TaskId not set");
                _taskDefinition = ParentProcess.Definition.GetTask(this.TaskId);
                log = LogManager.GetCurrentClassLogger();
                if (_taskDefinition == null) throw new Exception("Task definition not found: " + this.TaskId);
                if (_taskState != null)
                {
                    log.Debug("TaskShell {0}: Restoring task state from {1}", CorrelationId, _taskState.ToString());
                    IActiveTask at = RestoreTaskState(_taskState, _taskDefinition);
                    ActiveTask = at;
                }
                if (ActiveTask != null)
                {
                    ActiveTask.SetContext(this);
                    ActiveTask.Activate();
                }
                _activated = true;
            }
        }

        public virtual void Passivate()
        {
            if (ActiveTask != null)
            {
                ActiveTask.Passivate();
            }
            _parentProces = null;
            _taskDefinition = null;
            _parentCallback = null;
            _activated = false;
        }

        public bool IsActivated
        {
            get { return _activated; }
        }

        
        public IDataObject GetTaskData()
        {
            ActivationRequired(true);
            return ActiveTask.GetTaskData();
        }

        public virtual bool HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            return ActiveTask.HandleInternalTransitionEvent(ite);
        }

        protected void ActivationRequired(bool activated)
        {
            if (_activated != activated) throw new Exception();
        }

        public virtual void NotifyTransitionSelected()
        {
            ActivationRequired(true);
            ActiveTask.NotifyTransitionSelected();
            _parentCallback.TransitionStarted(this.CorrelationId);
        }


        #region IActiveTaskContext Members

        public Task TaskDefinition
        {
            get { return _taskDefinition; }
        }
        void IActiveTaskContext.TransitionStarted(string correlationId)
        {
            Debug.Assert(CorrelationId == correlationId);
            if (Status != TransitionStatus.ENABLED)
            {
                return;
            }
            ParentCallback.TransitionStarted(this.CorrelationId);
        }

        void IActiveTaskContext.TransitionCompleted(string correlationId, IDataObject taskOutputData)
        {
            Debug.Assert(CorrelationId == correlationId);
            Debug.Assert(taskOutputData != null);
            this._taskOutputData = taskOutputData;
            ParentCallback.TransitionCompleted(this.CorrelationId);
        }

        public Logger Log
        {
            get { return log; }
        }

        public override string ToString()
        {
            return string.Format("[Task: {0}, Status: {1}, CorrelationId: {2}]", this.TaskId, CorrelationId, Status);
        }

        public virtual DataObject SaveState()
        {
            DataObject dob = new DataObject();
            dob["Type"] = GetType().Name;
            dob["CorrelationId"] = CorrelationId;
            dob["TaskId"] = TaskId;
            dob["Status"] = Status.ToString();
            dob["SharedId"] = SharedId;
            List<string> ls = new List<string>();
            if (AllocatedPlaces != null)
            {
                foreach (string tok in this.AllocatedPlaces)
                {
                    ls.Add(tok);
                }
            }
            dob["AllocatedPlaces"] = ls;
            if (ActiveTask != null)
            {
                dob["Task"] = SaveTaskState(ActiveTask);
            }
            return dob;
        }

        /// <summary>
        /// Save the state of ActiveTask
        /// Use INGinnPersinstent if supported. If not, use binary serialization.
        /// </summary>
        /// <returns></returns>
        protected DataObject SaveTaskState(IActiveTask at)
        {
            if (at == null) return null;
            INGinnPersistent pers = at as INGinnPersistent;
            if (pers != null)
            {
                return pers.SaveState();
            }
            else
            {
                DataObject dob = new DataObject();
                dob["NGINN_BINARY_DATA"] = "Here goes binary serialized task";
                return dob;
            }
        }

        /// <summary>
        /// Restore task's state from DataObject.
        /// Use INGinnPersistent if supported. If not, use binary serialization.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected IActiveTask RestoreTaskState(DataObject data, Task definition)
        {
            string bs = (string)data["NGINN_BINARY_DATA"];
            if (bs != null)
            {
                //binary restore here
            }
            //restore activetask here...
            IActiveTask at = EnvironmentContext.ActiveTaskFactory.CreateActiveTask(definition);
            INGinnPersistent pers = at as INGinnPersistent;
            if (pers != null)
            {
                pers.RestoreState(data);
                return at;
            }
            log.Error("Don't know how to restore state of task {0} from {1}", at.GetType().FullName, data.ToString());
            throw new Exception(string.Format("Don't know how to restore state of task {0}", at.GetType().FullName));
        }

        /// <summary>
        /// Restore task shell's state
        /// </summary>
        /// <param name="state"></param>
        public virtual void RestoreState(DataObject state)
        {
            RequireActivation(false);
            CorrelationId = (string)state["CorrelationId"];
            TaskId = (string)state["TaskId"];
            _status = (TransitionStatus)Enum.Parse(typeof(TransitionStatus), (string)state["Status"]);
            SharedId = (string)state["SharedId"];
            IList lst = state.GetArray("AllocatedPlaces");
            if (lst != null)
            {
                List<string> ls = new List<string>();
                foreach (string s in lst) ls.Add(s);
                SetAllocatedPlaces(ls);
            }
            _taskState = (DataObject)state["Task"];
        }

        public static TaskShell RestoreTaskShell(DataObject state)
        {
            string tname = (string)state["Type"];
            TaskShell ts;
            if (tname == "MultiTaskShell")
            {
                ts = new MultiTaskShell();
            }
            else if (tname == "TaskShell")
            {
                ts = new TaskShell();
            }
            else throw new Exception();
            ts.RestoreState(state);
            return ts;
        }

        #endregion

        
    }
}

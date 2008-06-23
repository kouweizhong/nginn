using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using System.Xml;
using System.Xml.Schema;
using NGinn.Engine.Runtime;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;
using ScriptNET;

namespace NGinn.Engine.Runtime.Tasks
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
        protected ITransitionCallback _containerCallback;
        [NonSerialized]
        protected Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        private bool _activated = false;
        /// <summary>task data</summary>
        private DataObject _taskData = new DataObject();
        /// <summary>Serialized task xml data</summary>
        private string _taskDataXml;
        
        public ActiveTransition(Task tsk, ProcessInstance pi)
        {
            this.Status = TransitionStatus.ENABLED;
            this.TaskId = tsk.Id;
            this._processInstance = pi;
            this.ProcessInstanceId = pi.InstanceId;
        }

        public virtual void SetProcessInstance(ProcessInstance pi)
        {
            if (this.ProcessInstanceId != pi.InstanceId) throw new ApplicationException("Invalid process instance ID");
            this._processInstance = pi;
        }

        /// <summary>
        /// Task correlation id. Uniquely identifies the task instance.
        /// </summary>
        public string CorrelationId
        {
            get { return _correlationId; }
            set { ActivationRequired(false); _correlationId = value; }
        }

        /// <summary>
        /// Called after deserialization
        /// </summary>
        public virtual void Activate()
        {
            if (_processInstance == null) throw new ApplicationException("Process instance not set (call SetProcessInstance before activating)");
            if (_containerCallback == null) _containerCallback = (ITransitionCallback)_processInstance;
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

        /// <summary>
        /// Current transition's task definition
        /// </summary>
        protected Task ProcessTask
        {
            get { ActivationRequired(true); return _processInstance.Definition.GetTask(TaskId); }
        }

        
        protected StructDef GetTaskInternalDataSchema()
        {
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ProcessTask.ParentProcess.DataTypes;
            foreach (VariableDef vd in ProcessTask.TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    sd.Members.Add(vd);
                }
                else
                {
                    VariableDef vd2 = new VariableDef(vd); vd2.IsRequired = false;
                    sd.Members.Add(vd2);
                }
            }
            return sd;
        }

        /// <summary>
        /// Return container for task variables
        /// </summary>
        /// <returns></returns>
        protected IDataObject GetTaskVariablesContainer()
        {
            return _taskData;
        }

        protected IScriptContext CreateTaskScriptContext()
        {
            IScriptContext ctx = new ScriptContext();
            ctx.SetItem("_taskDef", ContextItem.Variable, ProcessTask);
            ctx.SetItem("_task", ContextItem.Variable, this);
            ctx.SetItem("_log", ContextItem.Variable, log);
            IDataObject dob = GetTaskVariablesContainer();
            foreach (string fn in dob.FieldNames)
            {
                ctx.SetItem(fn, ContextItem.Variable, dob[fn]);
            }
            return ctx;
        }

        public virtual void SetTaskInputData(IDataObject inputData)
        {
            StructDef sd = ProcessTask.GetTaskInputDataSchema();
            inputData.Validate(sd);
            DataObject taskData = new DataObject();
            IScriptContext ctx = CreateTaskScriptContext();
            ctx.SetItem("data", ContextItem.Variable, new DOBMutant(taskData));

            foreach (VariableDef vd in ProcessTask.TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.In ||
                    vd.VariableDir == VariableDef.Dir.InOut)
                {
                    taskData[vd.Name] = inputData[vd.Name];
                }
                else
                {
                    if (vd.DefaultValueExpr != null && vd.DefaultValueExpr.Length > 0)
                    {
                        taskData[vd.Name] = Script.RunCode(vd.DefaultValueExpr, ctx); 
                    }
                }
            }
            StructDef internalSchema = GetTaskInternalDataSchema();
            taskData.Validate(internalSchema);
            _taskData = taskData;
        }

        public virtual IDataObject GetTaskOutputData()
        {
            StructDef sd = ProcessTask.GetTaskOutputDataSchema();
            DataObject dob = new DataObject(sd);
            IDataObject src = GetTaskVariablesContainer();
            foreach (VariableDef vd in this.ProcessTask.TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.InOut || vd.VariableDir == VariableDef.Dir.Out)
                {
                    object obj = src[vd.Name];
                    dob.Set(vd.Name, null, obj);
                }
            }
            dob.Validate();
            return dob;
        }

        /// <summary>
        /// Return current task data
        /// </summary>
        /// <returns></returns>
        public virtual IDataObject GetTaskData()
        {
            return GetTaskVariablesContainer();
        }

        /// <summary>
        /// Modify task data (replace variable values)
        /// </summary>
        /// <param name="dob"></param>
        public virtual void UpdateTaskData(IDataObject dob)
        {
            IDataObject vars = GetTaskVariablesContainer();
            foreach (string fld in dob.FieldNames)
            {
                vars[fld] = dob[fld];
            }
            StructDef sd = GetTaskInternalDataSchema();
            vars.Validate(sd);
        }

        /// <summary>
        /// Initiate task (start the transition).
        /// If the transition is immediate, this operation will execute the task.
        /// If the transition is not immediate, this will initiate the transition.
        /// Subclasses should override this function, but should always call base.InitiateTask()
        /// </summary>
        public virtual void InitiateTask()
        {
            ActivationRequired(true);
            this.Status = TransitionStatus.ENABLED;
            DoInitiateTask();
        }


        protected abstract void DoInitiateTask();

        /// <summary>
        /// Check if task is immediate
        /// </summary>
        public virtual bool IsImmediate
        {
            get
            {
                return this.ProcessTask.IsImmediate;
            }
        }

        /// <summary>
        /// Execute an immediate task
        /// </summary>
        public virtual void ExecuteTask()
        {
            ActivationRequired(true);
            if (!IsImmediate) throw new ApplicationException("Execute is allowed only for immediate task");
            DoExecuteTask();
            this.Status = TransitionStatus.COMPLETED;
            this._containerCallback.TransitionCompleted(this.CorrelationId);
        }

        /// <summary>
        /// Abstract method. Execute the immediate task.
        /// </summary>
        protected abstract void DoExecuteTask();

        /// <summary>
        /// Invoked by runtime to cancel an active transition
        /// </summary>
        public virtual void CancelTask()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED && Status != TransitionStatus.STARTED)
                throw new ApplicationException("Cannot cancel task - status invalid");
            DoCancelTask();
            this.Status = TransitionStatus.CANCELLED;
        }

        protected abstract void DoCancelTask();

        protected void ActivationRequired(bool activated)
        {
            if (_activated != activated)
            {
                throw new ApplicationException(activated ? "Task must be activated" : "Task must be passivated");
            }
        }

        /// <summary>
        /// Handle internal transition event.
        /// Override it to handle transition-specific internal events 
        /// received from message bus
        /// </summary>
        /// <param name="ite"></param>
        public virtual void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            if (ite.ProcessInstanceId != this.ProcessInstanceId) throw new ApplicationException("Invalid process instance id");
            if (ite.CorrelationId != this.CorrelationId) throw new ApplicationException("Invalid correlation Id");
        }

        /// <summary>
        /// Notify the transition that it has been 'selected'
        /// By default, it does nothing but notifies the process instance
        /// that the transition has been selected
        /// </summary>
        public void NotifyTransitionSelected()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED &&
                this.Status != TransitionStatus.STARTED)
                throw new Exception("Status invalid");
            OnTransitionSelected();
            this.Status = TransitionStatus.STARTED;
            _containerCallback.TransitionStarted(this.CorrelationId);
        }

        /// <summary>
        /// override this method to handle 'transition selected'
        /// event. 
        /// </summary>
        protected virtual void OnTransitionSelected()
        {

        }

        /// <summary>
        /// Pass task completion information to active transition.
        /// As a result, task internal data is updated and
        /// OnTaskCompleted is invoked. Then the transition is marked
        /// as completed and TransitionCompleted callback is invoked.
        /// </summary>
        /// <param name="tci"></param>
        public void NotifyTaskCompleted(TaskCompletionInfo tci)
        {
            ActivationRequired(true);
            if (Status != TransitionStatus.STARTED &&
                Status != TransitionStatus.COMPLETED)
                throw new ApplicationException("Invalid transition status");
            if (tci.CorrelationId != this.CorrelationId)
                throw new ApplicationException("Invalid correlation id");
            if (tci.ResultXml != null)
            {
                DataObject dob = DataObject.ParseXml(tci.ResultXml);
                this.UpdateTaskData(dob);
            }
            OnTaskCompleted(tci);
            this.Status = TransitionStatus.COMPLETED;
            _containerCallback.TransitionCompleted(CorrelationId);
        }

        /// <summary>
        /// Handle task completion notification.
        /// Override this method to implement external
        /// notification about task completion.
        /// </summary>
        /// <param name="tci"></param>
        protected virtual void OnTaskCompleted(TaskCompletionInfo tci)
        {

        }
    }

    [Serializable]
    public class InternalTransitionEvent
    {
        public string ProcessInstanceId;
        public string CorrelationId;
    }
    
}

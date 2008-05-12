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

        public void SetTaskInputData(IDataObject inputData)
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

        public IDataObject GetTaskOutputData()
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
        public IDataObject GetTaskData()
        {
            return GetTaskVariablesContainer();
        }

        /// <summary>
        /// Modify task data (replace variable values)
        /// </summary>
        /// <param name="dob"></param>
        public void UpdateTaskData(IDataObject dob)
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
            if (this.Tokens.Count == 0) throw new Exception("No input tokens");
        }

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
        }

        /// <summary>
        /// Invoked by runtime to cancel an active transition
        /// </summary>
        public virtual void CancelTask()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED && Status != TransitionStatus.STARTED)
                throw new ApplicationException("Cannot cancel task - status invalid");
            this.Status = TransitionStatus.CANCELLED;
        }

        /// <summary>
        /// Invoked by runtime after transition has completed.
        /// </summary>
        public virtual void TaskCompleted()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED && this.Status != TransitionStatus.STARTED)
                throw new ApplicationException("Cannot complete task - status invalid");
            this.Status = TransitionStatus.COMPLETED;
        }

        protected void ActivationRequired(bool activated)
        {
            if (_activated != activated)
            {
                throw new ApplicationException(activated ? "Task must be activated" : "Task must be passivated");
            }
        }
    }
}

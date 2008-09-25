using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
using NGinn.Lib.Schema;
using NLog;
using System.Reflection;
using System.Diagnostics;
using ScriptNET;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Base class for implementing workflow tasks.
    /// In most simple case, new process task needs to implement
    /// only the abstract methods of ActiveTaskBase.
    /// Information: immediate tasks don't need to be serializable (they will not be serialized)
    /// </summary>
    [Serializable]
    public abstract class ActiveTaskBase : IActiveTask
    {
        [NonSerialized]
        protected Logger log;

        private bool _activated = false;
        private string _correlationId; 
        [NonSerialized]
        private IActiveTaskContext _ctx;

        private DataObject _taskData = new DataObject();

        public ActiveTaskBase(Task tsk)
        {
            log = LogManager.GetCurrentClassLogger();
        }

        #region IActiveTask Members

        public void SetContext(IActiveTaskContext ctx)
        {
            _ctx = ctx;
            log = ctx.Log;
        }

        public string CorrelationId
        {
            get { return _correlationId; }
            set { _correlationId = value; }
        }

        protected IActiveTaskContext Context
        {
            get { return _ctx; }
        }

        protected DataObject VariablesContainer
        {
            get { return _taskData; }
            set { _taskData = value; }
        }

        public virtual void Activate()
        {
            if (_ctx == null) throw new Exception("Context not set");
            if (_correlationId == null || _correlationId.Length == 0) throw new Exception("Correlation ID not set");
            _activated = true;
        }

        public virtual void Passivate()
        {
            _activated = false;
            _ctx = null;
        }

        public abstract void CancelTask();

        
        public virtual bool IsImmediate
        {
            get { return _ctx.TaskDefinition.IsImmediate; }
        }

        

        

        public virtual NGinn.Lib.Data.IDataObject GetTaskData()
        {
            return new DataObject(VariablesContainer);
        }

        /// <summary>
        /// TODO: implement
        /// </summary>
        /// <param name="dob"></param>
        public virtual void UpdateTaskData(NGinn.Lib.Data.IDataObject dob)
        {
            DataObject td = VariablesContainer;
            foreach (string fn in dob.FieldNames)
            {
                td[fn] = dob[fn];
            }
            StructDef internalSchema = Context.TaskDefinition.GetTaskInternalDataSchema();
            td.Validate(internalSchema);
        }

        /// <summary>
        /// Create script evaluation context for given variable set
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        protected IScriptContext CreateScriptContext(IDataObject variables)
        {
            IScriptContext ctx = new ScriptContext();
            DataObject env = new DataObject(Context.Environment.EnvironmentVariables);
            env["log"] = log;
            env["messageBus"] = Context.Environment.MessageBus;
            env["environment"] = Context.Environment;
            env["taskDefinition"] = Context.TaskDefinition;
            ctx.SetItem("__env", ContextItem.Variable, env);
            if (variables != null)
            {
                foreach (string fn in variables.FieldNames)
                {
                    ctx.SetItem(fn, ContextItem.Variable, variables[fn]);
                }
            }
            return ctx;
        }


        public virtual IList<TaskParameterInfo> GetTaskInputParameters()
        {
            List<TaskParameterInfo> lst = new List<TaskParameterInfo>();
            foreach (PropertyInfo pi in GetType().GetProperties())
            {
                object[] attrs = pi.GetCustomAttributes(typeof(TaskParameterAttribute), false);
                if (attrs.Length > 0)
                {
                    TaskParameterAttribute tpa = (TaskParameterAttribute) attrs[0];
                    if (tpa.IsInput)
                    {
                        TaskParameterInfo tpi = new TaskParameterInfo(pi.Name, pi.PropertyType, tpa.Required, tpa.IsInput, tpa.DynamicAllowed);
                        tpi.ParameterType = pi.PropertyType;
                        lst.Add(tpi);
                    }
                }
            }
            return lst;
        }

        public virtual void SetTaskParameterValue(string paramName, object value)
        {
            PropertyInfo pi = GetType().GetProperty(paramName);
            if (pi == null) throw new ApplicationException("Parameter not found: " + paramName);
            object v = Convert.ChangeType(value, pi.PropertyType);
            pi.SetValue(this, v, null);
        }

        public virtual object GetTaskParameterValue(string paramName)
        {
            PropertyInfo pi = GetType().GetProperty(paramName);
            if (pi != null) return pi.GetValue(this, null);
            return null;
        }
        #endregion

        #region IActiveTask Members


        public virtual void NotifyTransitionSelected()
        {
            Context.TransitionStarted(this.CorrelationId);
        }

       
        /// <summary>
        /// Default implementation of internal transition event handler.
        /// Currently it handles TaskCompletedNotification and TransitionSelectedNotification.
        /// Override it to handle other event types.
        /// </summary>
        /// <param name="ite"></param>
        public virtual void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            if (ite is TaskCompletedNotification)
            {
                DefaultHandleTaskCompletedEvent((TaskCompletedNotification)ite);
            }
        }

        /// <summary>
        /// Default 'task completed' handler. 
        /// </summary>
        /// <param name="ev"></param>
        protected void DefaultHandleTaskCompletedEvent(TaskCompletedNotification ev)
        {
            if (ev.TaskData != null)
            {
                UpdateTaskData(ev.TaskData);
            }
            OnTaskCompleted();
        }

        /// <summary>
        /// Implementation of InitiateTask. Provides input data validation and task parameter 
        /// initialization, so don't override it if you want to use standard mechanism. Implement
        /// DoInitiateTask to provide your task logic.
        /// </summary>
        /// <param name="inputData"></param>
        public virtual void InitiateTask(IDataObject inputData)
        {
            InitializeTaskData(inputData);
            InitateTakskParameters(inputData);
            DoInitiateTask();
        }

        /// <summary>
        /// Implement this method to execute task logic.
        /// If the task does not complete immediately, just return.
        /// If the task completes synchronously, call OnTaskCompleted before returning.
        /// </summary>
        protected abstract void DoInitiateTask();

        /// <summary>
        /// Set-up task parameter values according to parameter bindings.
        /// Call this function at the beginning of InitiateTask so the task 
        /// parameters are initalized.
        /// </summary>
        /// <param name="inputData"></param>
        protected void InitateTakskParameters(IDataObject inputData)
        {
            IList<TaskParameterInfo> inputs = GetTaskInputParameters();
            Dictionary<string, TaskParameterInfo> paramDict = new Dictionary<string,TaskParameterInfo>();
            foreach(TaskParameterInfo tpi in inputs) paramDict[tpi.Name] = tpi;

            IList<TaskParameterBinding> bindings = Context.TaskDefinition.ParameterBindings;
            IScriptContext ctx = CreateScriptContext(inputData);

            foreach (TaskParameterBinding tb in bindings)
            {
                TaskParameterInfo tpi;
                if (!paramDict.TryGetValue(tb.PropertyName, out tpi))
                    throw new ApplicationException("Parameter not found: " + tb.PropertyName);
                
                if (tb.BindingType == TaskParameterBinding.ParameterBindingType.Value)
                {
                    SetTaskParameterValue(tb.PropertyName, tb.BindingExpression);
                }
                else if (tb.BindingType == TaskParameterBinding.ParameterBindingType.Expr)
                {
                    string code = tb.BindingExpression.Trim();
                    if (!code.EndsWith(";")) code += ";";
                    try
                    {
                        object v = Script.RunCode(code, ctx);
                        SetTaskParameterValue(tb.PropertyName, v);
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException(string.Format("Failed to evaluate binding of parameter '{0}' in task '{1}'", tb.PropertyName, this.CorrelationId), ex);
                    }
                }
                else throw new Exception("Binding type not supported: " + tb.BindingType);
                paramDict.Remove(tpi.Name);
            }
            foreach(string k in paramDict.Keys)
            {
                TaskParameterInfo tpi = paramDict[k];
                if (tpi.Required) throw new ApplicationException("Required task parameter not initialized: " + tpi.Name);
            }
        }

        /// <summary>
        /// Prepare task data - validate the input and initialize task variables. Throws exception if input data structure
        /// does not validate against task data schema.
        /// </summary>
        /// <param name="inputData"></param>
        protected void InitializeTaskData(IDataObject inputData)
        {
            StructDef sd = Context.TaskDefinition.GetTaskInputDataSchema();
            inputData.Validate(sd);
            DataObject taskData = new DataObject();
            IScriptContext ctx = this.CreateScriptContext(null); 
            ctx.SetItem("data", ContextItem.Variable, new DOBMutant(taskData));

            foreach (VariableDef vd in Context.TaskDefinition.TaskVariables)
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
                        string code = vd.DefaultValueExpr.Trim();
                        if (!code.EndsWith(";")) code += ";";
                        taskData[vd.Name] = Script.RunCode(code, ctx);
                    }
                }
            }
            StructDef internalSchema = Context.TaskDefinition.GetTaskInternalDataSchema();
            taskData.Validate(internalSchema);
            this._taskData = taskData;
        }

        /// <summary>
        /// Return task output data.
        /// Performs output data validation
        /// </summary>
        /// <returns></returns>
        public virtual IDataObject GetOutputData()
        {
            StructDef sd = Context.TaskDefinition.GetTaskOutputDataSchema();
            DataObject dob = new DataObject(sd);
            IDataObject src = VariablesContainer;
            foreach (VariableDef vd in Context.TaskDefinition.TaskVariables)
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

        #endregion

        /// <summary>
        /// Call this method when the task completes.
        /// It handles output data and notifying the container that the task has completed
        /// TODO:implement
        /// </summary>
        protected void OnTaskCompleted()
        {
            IDataObject dob = GetOutputData();
            Context.TransitionCompleted(this.CorrelationId, dob); 
        }
    }
}

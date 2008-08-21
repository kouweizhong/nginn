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
        private bool _activated = false;
        private string _correlationId; 
        [NonSerialized]
        private IActiveTaskContext _ctx;

        private DataObject _taskData = new DataObject();

        public ActiveTaskBase(Task tsk)
        {
        }

        #region IActiveTask Members

        public void SetContext(IActiveTaskContext ctx)
        {
            _ctx = ctx;
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

        

        public virtual NGinn.Lib.Data.IDataObject GetOutputData()
        {
            throw new NotImplementedException();
        }

        public virtual NGinn.Lib.Data.IDataObject GetTaskData()
        {
            return VariablesContainer;
        }

        public virtual void UpdateTaskData(NGinn.Lib.Data.IDataObject dob)
        {
            throw new NotImplementedException();
        }

        protected IScriptContext CreateScriptContext(IDataObject variables)
        {
            IScriptContext ctx = new ScriptContext();
            foreach (string fn in variables.FieldNames)
            {
                ctx.SetItem(fn, ContextItem.Variable, variables[fn]);
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
        {}

       
        public virtual void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            
        }

        public abstract void InitiateTask(IDataObject inputData);

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
                    object v = Script.RunCode(tb.BindingExpression, ctx);
                    SetTaskParameterValue(tb.PropertyName, v);
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
        /// Validate task input data. Throws exception if input data structure
        /// does not validate against task data schema.
        /// </summary>
        /// <param name="inputData"></param>
        protected void ValidateInputData(IDataObject inputData)
        {

        }
            

        #endregion

        /// <summary>
        /// Call this method when the task completes.
        /// It handles output data and notifying the container that the task has completed
        /// TODO:implement
        /// </summary>
        protected void OnTaskCompleted()
        {
            DataObject dob = new DataObject(GetTaskData());
            Context.TransitionCompleted(this.CorrelationId, dob); 
        }
    }
}

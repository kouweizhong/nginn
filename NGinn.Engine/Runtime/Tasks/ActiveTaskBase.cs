using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
using NGinn.Lib.Schema;
using NLog;
using System.Reflection;
using System.Diagnostics;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Base class for implementing workflow tasks.
    /// In most simple case, new process task needs to implement
    /// only the abstract methods of ActiveTaskBase.
    /// Information: immediate tasks don't need to be serializable (they will not be serialized)
    /// </summary>
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
            Debug.Assert(CorrelationId == ctx.CorrelationId);
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
            _activated = true;
        }

        public virtual void Passivate()
        {
            _activated = false;
            _ctx = null;
        }

        public abstract void CancelTask();

        public abstract void InitiateTask();
        
        public abstract void ExecuteTask();
        
        public virtual bool IsImmediate
        {
            get { return _ctx.TaskDefinition.IsImmediate; }
        }

        public virtual void SetInputData(NGinn.Lib.Data.IDataObject dob)
        {
            throw new NotImplementedException();
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
                        TaskParameterInfo tpi = new TaskParameterInfo(pi.Name, tpa.Required, tpa.DynamicAllowed);
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

        #endregion
    }
}

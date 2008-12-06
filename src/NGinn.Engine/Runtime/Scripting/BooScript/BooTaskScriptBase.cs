using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
using NLog;
using NGinn.Engine.Runtime;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime.Scripting.BooScript
{
    /// <summary>
    /// Base class for task script code.
    /// The script code will be automatically translated
    /// into class inheriting from TaskScriptBase.
    /// </summary>
    public class BooTaskScriptBase : ITaskScript
    {
        private IActiveTask _task;
        private INGEnvironmentContext _env;
        private IDataObject _data;
        private Logger _log = LogManager.GetCurrentClassLogger();

        public IActiveTask TaskInstance
        {
            get { return _task; }
            set { _task = value; }
        }

        
        public IDataObject SourceData
        {
            get { return _data; }
            set { _data = value; }
        }

        
        public Logger log
        {
            get { return _log; }
        }

        
        public INGEnvironmentContext Environment
        {
            get { return _env; }
            set { _env = value;  }
        }

        private IActiveTaskContext _taskCtx;

        public IActiveTaskContext TaskContext
        {
            get { return _taskCtx; }
            set { _taskCtx = value; }
        }

        /// <summary>
        /// Return value of input task parameter (for expressions only)
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public virtual object GetTaskParameterValue(string paramName)
        {
            return null;
        }

        /// <summary>
        /// Evaluate default value expression
        /// for a task variable
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public virtual object GetTaskVariableDefaultValue(string variableName)
        {
            return null;
        }

        public delegate void Action();
        public delegate object Expression();
        /// <summary>
        /// Evaluate task script
        /// </summary>
        /// <param name="scriptName"></param>
        public virtual void EvaluateScript(string scriptName)
        {

        }

        public object GetInputParameterValue(string paramName)
        {
            throw new NotImplementedException();
        }

        public object GetDefaultVariableValue(string variableName)
        {
            throw new NotImplementedException();
        }

        public object RunScriptBlock(string blockId)
        {
            throw new NotImplementedException();
        }

        public object EvalInputVariableBinding(string varName)
        {
            throw new NotImplementedException();
        }
        
        public object EvalOutputVariableBinding(string varName)
        {
            throw new NotImplementedException();
        }

        public object EvalMultiInstanceSplitQuery()
        {
            throw new NotImplementedException();
        }
    }
}

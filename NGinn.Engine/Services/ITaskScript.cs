using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
using NGinn.Engine.Runtime;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Interface for evaluating task scripts
    /// - data and parameter bindings
    /// - script task's body
    /// This interface is introduced to avoid coupling to single script evaluation engine
    /// and to isolate all script invocations.
    /// </summary>
    public interface ITaskScript
    {
        /// <summary>
        /// Script source data
        /// </summary>
        IDataObject SourceData
        {
            get;
            set;
        }

        /// <summary>
        /// Task context
        /// </summary>
        IActiveTaskContext TaskContext
        {
            get;
            set;
        }

        /// <summary>
        /// Task instance
        /// </summary>
        IActiveTask TaskInstance
        {
            get;
            set;
        }


        /// <summary>
        /// Get value of task input parameter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <returns></returns>
        object GetInputParameterValue(string paramName);

        /// <summary>
        /// Calculate default value of task variable
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        object GetDefaultVariableValue(string variableName);


        /// <summary>
        /// Evaluate script block
        /// </summary>
        /// <param name="blockId">Id of the block (not the script code)</param>
        /// <returns></returns>
        object RunScriptBlock(string blockId);

        /// <summary>
        /// Evaluate task input variable binding.
        /// Source data should be set to process variables.
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        object EvalInputVariableBinding(string varName);

        /// <summary>
        /// Evaluate task output data binding.
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        object EvalOutputVariableBinding(string varName);

        /// <summary>
        /// Evaluate multi instance task split query
        /// </summary>
        /// <returns></returns>
        object EvalMultiInstanceSplitQuery();
    }
}

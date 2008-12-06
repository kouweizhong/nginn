using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Interface of an evaluator of script expressions in process definition.
    /// This interface is introduced to avoid coupling to single script evaluation engine
    /// and to isolate all script invocations.
    /// </summary>
    public interface IProcessScript
    {
        /// <summary>
        /// Process instance data
        /// </summary>
        NGinn.Lib.Data.IDataObject ProcessData { get; set; }

        /// <summary>
        /// Process instance
        /// </summary>
        ProcessInstance Instance { get; set; }


        INGEnvironmentContext EnvironmentContext { get; set; }
        
        /// <summary>
        /// Calculate default value of task variable
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        object GetDefaultVariableValue(string variableName);

        /// <summary>
        /// Evaluate input condition of given flow
        /// </summary>
        /// <param name="fl"></param>
        /// <returns></returns>
        object EvaluateFlowInputCondition(Flow fl);
    }
}

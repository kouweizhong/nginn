using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;

namespace NGinn.Engine.Runtime
{
    public interface IActiveTaskContext
    {
        string CorrelationId { get; }
        Task TaskDefinition { get; }
        ProcessInstance ParentProcess { get; }
        TransitionStatus Status { get; }
        /// <summary>
        /// Called after the transition has been started
        /// This is optional callback. 
        /// </summary>
        /// <param name="correlationId"></param>
        void TransitionStarted(string correlationId);
        /// <summary>
        /// Called after the transition has been completed
        /// </summary>
        /// <param name="correlationId"></param>
        void TransitionCompleted(string correlationId);

            }

    /// <summary>
    /// Interface implemented by all active tasks
    /// </summary>
    public interface IActiveTask
    {
        /// <summary>
        /// Set task context
        /// </summary>
        /// <param name="ctx"></param>
        void SetContext(IActiveTaskContext ctx);
        /// <summary>
        /// Get/set task correlation Id
        /// </summary>
        string CorrelationId { get; set; }
        /// <summary>
        /// Activate task instance
        /// </summary>
        void Activate();
        /// <summary>
        /// Passivate task instance - prepare for serialization
        /// </summary>
        void Passivate();
        /// <summary>
        /// Cancel the task
        /// </summary>
        void CancelTask();

        void InitiateTask();
        void ExecuteTask();
        bool IsImmediate { get; }
        void SetInputData(IDataObject dob);
        IDataObject GetOutputData();
        IDataObject GetTaskData();
        void UpdateTaskData(IDataObject dob);
        /// <summary>
        /// Return list of task input parameters
        /// </summary>
        /// <returns></returns>
        IList<TaskParameterInfo> GetTaskInputParameters();
        void SetTaskParameterValue(string paramName, object value);
        object GetTaskParameterValue(string paramName);

        void NotifyTransitionSelected();

        void HandleInternalTransitionEvent(InternalTransitionEvent ite);
    }
}

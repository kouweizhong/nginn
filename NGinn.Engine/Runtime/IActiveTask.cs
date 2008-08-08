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
        /// <param name="taskOutputData"></param>
        void TransitionCompleted(string correlationId, DataObject taskOutputData);

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

        /// <summary>
        /// Initiate task
        /// In case of immediate tasks it should execute the task 
        /// and return results through 'Context.TransitionCompleted' callback.
        /// In case of non-immediate tasks, it should initiate the task execution.
        /// </summary>
        /// <param name="inputData">Task input data</param>
        void InitiateTask(IDataObject inputData);

        bool IsImmediate { get; }
        
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

        /// <summary>
        /// Notify task instance that the transition has been selected.
        /// </summary>
        void NotifyTransitionSelected();

        /// <summary>
        /// Pass internal transition event to task to handle it
        /// </summary>
        /// <param name="ite"></param>
        void HandleInternalTransitionEvent(InternalTransitionEvent ite);
    }
}

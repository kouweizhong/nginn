using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;
using NGinn.Engine;
using NLog;

namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// Context information available to active tasks
    /// </summary>
    public interface IActiveTaskContext
    {
        /// <summary>
        /// transition's correlation id
        /// Usually the same as task correlation id, different only in case of multi-instance tasks
        /// </summary>
        string CorrelationId { get; }
        /// <summary>
        /// task definition
        /// </summary>
        Task TaskDefinition { get; }
        /// <summary>
        /// parent process instance ID
        /// </summary>
        string ProcessInstanceId { get; }
        /// <summary>
        /// transition's status
        /// </summary>
        TransitionStatus Status { get; }
        /// <summary>
        /// environment where process is hosted
        /// </summary>
        INGEnvironmentContext EnvironmentContext { get; }
        /// <summary>
        /// Information about ID of implicit choice task group (so the tasks know
        /// that they belong to the same implicit choice group).
        /// </summary>
        string SharedId { get; }
        /// <summary>
        /// Notify task's container that the task has been started (selected for execution)
        /// This is optional callback. 
        /// </summary>
        /// <param name="correlationId"></param>
        void TransitionStarted(string correlationId);
        /// <summary>
        /// Notify task's container that the task has completed
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="taskOutputData"></param>
        void TransitionCompleted(string correlationId, IDataObject taskOutputData);

        /// <summary>
        /// Process instance logger
        /// </summary>
        Logger Log { get; }

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

       
        
        /// <summary>
        /// Return current task data
        /// </summary>
        /// <returns></returns>
        IDataObject GetTaskData();

        /// <summary>
        /// Update task data
        /// </summary>
        /// <param name="dob"></param>
        void UpdateTaskData(IDataObject dob);
        
        /// <summary>
        /// Return list of task input parameters
        /// </summary>
        /// <returns></returns>
        IList<TaskParameterInfo> GetTaskInputParameters();
        
        /// <summary>
        /// Set value of task parameter
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        void SetTaskParameterValue(string paramName, object value);

        /// <summary>
        /// Return value of task parameter
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        object GetTaskParameterValue(string paramName);

        /// <summary>
        /// Notify task instance that the transition has been selected.
        /// TODO: decide if this method is necessary
        /// </summary>
        void NotifyTransitionSelected();

        /// <summary>
        /// Pass internal transition event to task to handle it
        /// </summary>
        /// <param name="ite"></param>
        /// <returns>true if event was handled and false if event was not handled
        /// (did not modify anything)</returns>
        bool HandleInternalTransitionEvent(InternalTransitionEvent ite);
    }
}

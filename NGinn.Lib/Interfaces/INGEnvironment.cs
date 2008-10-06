using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;

namespace NGinn.Lib.Interfaces
{
    [Serializable]
    public class ProcessInstanceInfo
    {
        public string ProcessInstanceId;
        public string ProcessDefinitionId;
        public bool ProcessFinished;
    }

    [Serializable]
    public class TaskInstanceInfo
    {
        public string ProcessInstanceId;
        public string ProcessDefinitionId;
        public bool ProcessFinished;
        public string CorrelationId;
        public string TaskId;
        public bool TaskCompleted;
    }

    /// <summary>
    /// Process hosting environment interface
    /// </summary>
    public interface INGEnvironment
    {
        /// <summary>
        /// Start new process instance
        /// </summary>
        /// <param name="definitionId">Process definition ID (check IProcessDefinitionRepository)</param>
        /// <param name="inputXml">Process input variables</param>
        /// <param name="processCorrelationId">Process correlation id - externally assigned process identifier. Can be null. Uniqueness is not checked</param>
        /// <returns>new process instance ID</returns>
        string StartProcessInstance(string definitionId, string inputXml, string processCorrelationId);

        /// <summary>
        /// Start new process instance
        /// </summary>
        /// <param name="definitionId"></param>
        /// <param name="inputData"></param>
        /// <param name="userId">Id of user who starts the process</param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        string StartProcessInstance(string definitionId, IDataObject inputData, string userId, string correlationId);

        /// <summary>
        /// Get lists of processes that can be 'kicked', that is, have some work to do
        /// </summary>
        /// <returns>list of process instance IDs</returns>
        IList<string> GetKickableProcesses();

        /// <summary>
        /// Kick a process instance. If there is some work to do in process instance, execute some of that work
        /// </summary>
        /// <param name="instanceId">Process instance ID</param>
        void KickProcess(string instanceId);

        /// <summary>
        /// Set environment variable. Used for passing external information to process instances.
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">variable value</param>
        void SetEnvVariable(string name, object value);

        /// <summary>
        /// Return value of environment variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetEnvVariable(string name);

        

        /// <summary>
        /// Return xml document containing current process instance data
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        string GetProcessInstanceData(string instanceId);

        /// <summary>
        /// Return task instance data as XML
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        string GetTaskInstanceXml(string correlationId);

        /// <summary>
        /// Cancel process instance.
        /// </summary>
        /// <param name="instanceId"></param>
        void CancelProcessInstance(string instanceId);

        /// <summary>
        /// Return task instance data
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        DataObject GetTaskData(string correlationId);

        /// <summary>
        /// Return process output data. Can be called only after process completes.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        DataObject GetProcessOutputData(string instanceId);

        /// <summary>
        /// Dispatch message to process waiting for it.
        /// </summary>
        /// <param name="messageCorrelationId">Message ID, used for finding the task waiting for the message</param>
        /// <param name="messageBody">Message data content</param>
        void DispatchProcessMessage(string messageCorrelationId, DataObject messageBody);

        /// <summary>
        /// Inform NGInn process that a task has been completed.
        /// Used with manual tasks.
        /// </summary>
        /// <param name="correlationId">Task's correlation Id</param>
        /// <param name="updatedTaskData">Task variables to update</param>
        /// <param name="userId">Id of user completing the task</param>
        void ReportTaskFinished(string correlationId, DataObject updatedTaskData, string userId);

        /// <summary>
        /// Notify process that task execution has started (user has started 
        /// executing manual task)
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="userId"></param>
        void NotifyTaskExecutionStarted(string correlationId, string userId);
        /// <summary>
        /// Return information about a process instance
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        ProcessInstanceInfo GetProcessInstanceInfo(string instanceId);
        
        /// <summary>
        /// Return information about a task instance
        /// </summary>
        /// <param name="taskCorrelationId"></param>
        /// <returns></returns>
        TaskInstanceInfo GetTaskInstanceInfo(string taskCorrelationId);
    }
}
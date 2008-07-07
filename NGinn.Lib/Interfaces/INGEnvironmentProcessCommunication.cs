using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
namespace NGinn.Lib.Interfaces
{
    /// <summary>
    /// Task completion data. Contains task execution results.
    /// </summary>
    [Serializable]
    public class TaskCompletionInfo
    {
        /// <summary>Process instance ID</summary>
        public string ProcessInstance;
        /// <summary>Task correlation ID</summary>
        public string CorrelationId;
        /// <summary>ID of person that completed the task</summary>
        public string CompletedBy;
        /// <summary>Task completion date</summary>
        public DateTime CompletedDate = DateTime.Now;
        /// <summary>Task results xml</summary>
        public string ResultXml;
    }

    public interface INGEnvironmentProcessCommunication
    {
        /// <summary>
        /// Inform the engine that task in process has been completed
        /// </summary>
        /// <param name="info"></param>
        void ProcessTaskCompleted(TaskCompletionInfo info);

        /// <summary>
        /// Inform the engine that task has been selected for processing.
        /// This will cancel all deferred choice alternatives for this task.
        /// Use of this method is optional - if it is not called, the alternatives will
        /// be cancelled when the task is completed (ProcessTaskCompleted is called)
        /// </summary>
        /// <param name="correlationId"></param>
        void ProcessTaskSelectedForProcessing(string processInstanceId, string correlationId);

    }
}

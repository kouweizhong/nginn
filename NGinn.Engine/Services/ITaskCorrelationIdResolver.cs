using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Registry that maps some ID to task correlation ID.
    /// Used for routing messages to proper ReceiveMessageTasks.
    /// For example: we send out email with process instanceID in subject and 
    /// set up ReceiveMessageTask that will wait for the response.
    /// Email response contains original email's subject with embedded process instanceId,
    /// so we can find proper receiveMessageTask with this instanceId (provided
    /// that there's only one receiveMessageTask for that process)
    /// Mappings held by resolver are persistent.
    /// </summary>
    public interface ITaskCorrelationIdResolver
    {
        /// <summary>
        /// register mapping of some ID to task correlation id
        /// Id must be unique.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="taskCorrelationId"></param>
        void RegisterMapping(string id, string taskCorrelationId);
        string GetCorrelationId(string id);
        void RemoveMapping(string id, string taskCorrelationId);
    }
}

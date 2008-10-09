using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine;
using NGinn.Engine.Services.Dao;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Process instance repository interface. Contains functions for persisting 
    /// process instances and accessing persisted processes.
    /// </summary>
    public interface IProcessInstanceRepository
    {
        /// <summary>
        /// Return list of processes that can be kicked (have tokens that could be processed).
        /// </summary>
        /// <returns></returns>
        IList<string> SelectProcessesWithReadyTokens();

        /// <summary>
        /// Retrieve process instance from a repository
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        ProcessInstance GetProcessInstance(string instanceId);

        /// <summary>
        /// Update existing process instance
        /// </summary>
        /// <param name="pi"></param>
        void UpdateProcessInstance(ProcessInstance pi);
        /// <summary>
        /// Set process instance status to 'error'.
        /// In this case engine will retry processing this instance
        /// several times before giving up.
        /// Warning: this function is used only when handling ready
        /// tokens - if ready token cannot move forward due to error the
        /// engine will later retry. It is not used when handling task completion - in that
        /// case we rely on message bus to handle retrying.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="errorInfo"></param>
        void SetProcessInstanceErrorStatus(string instanceId, string errorInfo);

        /// <summary>
        /// Insert new process to the repository
        /// </summary>
        /// <param name="pi"></param>
        void InsertNewProcessInstance(ProcessInstance pi);

        /// <summary>
        /// Searches process instance database for processes with specified 
        /// external id. Returns a list of process instance IDs
        /// </summary>
        /// <param name="extid"></param>
        /// <returns></returns>
        IList<string> FindProcessesByExternalId(string extid);
    }
}

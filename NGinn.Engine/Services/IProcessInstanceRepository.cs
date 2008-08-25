using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine;
using NGinn.Engine.Services.Dao;

namespace NGinn.Engine.Services
{
    public interface IProcessInstanceRepository
    {
        /// <summary>
        /// Retrieve process instance from a repository
        /// </summary>
        /// <param name="instanceId">Process instance ID</param>
        /// <param name="ds">data session</param>
        /// <returns></returns>
        ProcessInstance GetProcessInstance(string instanceId, INGDataSession ds);

        void UpdateProcessInstance(ProcessInstance pi, INGDataSession ds);
        ProcessInstance InitializeNewProcessInstance(string definitionId, INGDataSession ds);
        IList<string> SelectProcessesWithReadyTokens();
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
        void SetProcessInstanceErrorStatus(string instanceId, string errorInfo, INGDataSession ds);
    }
}

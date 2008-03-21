using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine;
using NGinn.Engine.Services.Dao;

namespace NGinn.Engine.Services
{
    public interface IProcessInstanceRepository
    {
        ProcessInstance GetProcessInstance(string instanceId, INGDataSession ds);
        void UpdateProcessInstance(ProcessInstance pi, INGDataSession ds);
        ProcessInstance InitializeNewProcessInstance(string definitionId, INGDataSession ds);
        IList<Token> GetProcessActiveTokens(string instanceId, INGDataSession ds);
        IList<string> SelectProcessesWithReadyTokens();
    }
}

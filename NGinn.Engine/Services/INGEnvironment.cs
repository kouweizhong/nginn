using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    public interface INGEnvironment
    {
        string StartProcessInstance(string definitionId, IDictionary<string, object> inputVariables);
        IList<string> GetKickableProcesses();
        void KickProcess(string instanceId);
        
    }
}

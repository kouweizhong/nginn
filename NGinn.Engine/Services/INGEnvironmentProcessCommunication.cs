using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    [Serializable]
    public class TaskCompletionInfo
    {
        public string ProcessInstance;
        public string CorrelationId;
        public string ResultCode;
        public string CompletedBy;
        public DateTime CompletedDate = DateTime.Now;
        public IDictionary<string, object> OutputVariables = new Dictionary<string, object>();
    }

    public interface INGEnvironmentProcessCommunication
    {
        /// <summary>
        /// Inform the engine that task in process has been completed
        /// </summary>
        /// <param name="info"></param>
        void ProcessTaskCompleted(TaskCompletionInfo info);
    }
}

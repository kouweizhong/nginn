using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;

namespace NGinn.Engine.Dao
{
    public class CachingProcessInstanceRepository : IProcessInstanceRepository
    {
        private IProcessInstanceRepository _backend;
        private Dictionary<string, ProcessInstance> _cache = new Dictionary<string, ProcessInstance>();

        public CachingProcessInstanceRepository()
        {
            _backend = new ProcessInstanceRepository();
        }


        public IProcessInstanceRepository BackEndRepository
        {
            get { return _backend; }
            set { _backend = value; }
        }





        #region IProcessInstanceRepository Members

        public ProcessInstance GetProcessInstance(string instanceId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            lock (this)
            {
                ProcessInstance pi;
                if (_cache.TryGetValue(instanceId, out pi)) return pi;
                pi = BackEndRepository.GetProcessInstance(instanceId, ds);
                _cache[instanceId] = pi;
                return pi;
            }
        }

        public void UpdateProcessInstance(ProcessInstance pi, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            lock (this)
            {
                if (_cache.ContainsKey(pi.InstanceId)) _cache.Remove(pi.InstanceId);
                BackEndRepository.UpdateProcessInstance(pi, ds);
            }
        }

        public ProcessInstance InitializeNewProcessInstance(string definitionId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            return BackEndRepository.InitializeNewProcessInstance(definitionId, ds);
        }

        public IList<string> SelectProcessesWithReadyTokens()
        {
            return BackEndRepository.SelectProcessesWithReadyTokens();
        }

        public void SetProcessInstanceErrorStatus(string instanceId, string errorInfo, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            BackEndRepository.SetProcessInstanceErrorStatus(instanceId, errorInfo, ds);
        }

        #endregion
    }
}

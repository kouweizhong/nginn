using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using System.Collections;

namespace NGinn.Engine.Runtime
{
    class LocalProcessInstanceLockManager : IProcessInstanceLockManager
    {
        private Hashtable _locks = new Hashtable();

        #region IProcessInstanceLockManager Members

        public bool TryAcquireLock(string instanceId)
        {
            lock (this)
            {
                if (_locks.ContainsKey(instanceId)) return false;
                _locks[instanceId] = instanceId;
                return true;
            }
        }

        public void ReleaseLock(string instanceId)
        {
            lock (this)
            {
                _locks.Remove(instanceId);
            }
        }

        #endregion
    }
}

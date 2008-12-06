using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using System.Collections;

namespace NGinn.Engine.Runtime
{
    class LocalProcessInstanceLockManager : IProcessInstanceLockManager
    {
        private PoolingResourceLockManager _mgr = new PoolingResourceLockManager();

        #region IProcessInstanceLockManager Members

        public IResourceLock AcquireReaderLock(string instanceId, TimeSpan timeout)
        {
            return _mgr.AcquireReaderLock(instanceId, timeout);
        }

        public IResourceLock AcquireWriterLock(string instanceId, TimeSpan timeout)
        {
            return _mgr.AcquireWriterLock(instanceId, timeout);
        }

        #endregion
    }
}

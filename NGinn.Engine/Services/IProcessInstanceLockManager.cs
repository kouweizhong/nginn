using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    public interface IProcessInstanceLockManager
    {
        /// <summary>
        /// Try to acquire a lock on process instance
        /// </summary>
        /// <param name="instanceId">Process instance ID</param>
        /// <param name="timeout">Lock timeout. If timeout=0, lock will be acquired only if process instance
        /// is not currently locked. If timeout > 0, function will wait no more that specified timeout</param>
        /// <returns></returns>
        bool TryAcquireLock(string instanceId, int timeout);
        void ReleaseLock(string instanceId);
    }
}

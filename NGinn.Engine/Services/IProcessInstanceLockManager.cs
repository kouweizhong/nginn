using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    public interface IProcessInstanceLockManager
    {
        bool TryAcquireLock(string instanceId);
        void ReleaseLock(string instanceId);
    }
}

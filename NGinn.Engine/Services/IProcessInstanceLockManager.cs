using System;
using System.Collections.Generic;
using System.Text;
using Spring.Threading;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Resource lock interface
    /// </summary>
    public interface IResourceLock : IDisposable
    {
        void Release();
    }


    /// <summary>
    /// Process instance locking interface
    /// </summary>
    public interface IProcessInstanceLockManager
    {
        /// <summary>
        /// Acquire reader lock for specified instance
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        IResourceLock AcquireReaderLock(string instanceId, TimeSpan timeout);
        /// <summary>
        /// Acquire write lock for specified process instance
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        IResourceLock AcquireWriterLock(string instanceId, TimeSpan timeout);
    }
}

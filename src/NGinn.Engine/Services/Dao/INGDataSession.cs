using System;
using System.Collections.Generic;
using System.Text;
using Spring.Threading;

namespace NGinn.Engine.Services.Dao
{
    public interface INGDataSession : IDisposable
    {
        void Commit();
        void Rollback();
        /// <summary>
        /// Acquire a global process instance lock. The lock returned can be 
        /// a reader lock (write == false) or writer lock (write == true).
        /// Lock will be released when the session is closed.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="write"></param>
        /// <returns></returns>
        ISync LockProcessInstance(string instanceId, bool write);
    }
}

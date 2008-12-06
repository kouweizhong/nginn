using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services.Dao;
using Sooda;
using Spring.Threading;

namespace NGinn.Engine.Dao
{
    public class NGDataStore : INGDataStore
    {
        #region INGDataStore Members
        

        public INGDataSession OpenSession()
        {
            return new SoodaSession();
        }

        #endregion
    }

    internal class LockInfo
    {
        public string InstanceId;
        public bool IsWriter;
        public ISync TheLock;
    }

    public class SoodaSession : INGDataSession
    {
        private SoodaTransaction _st;

        

        private Dictionary<string, LockInfo> _sessionLocks = new Dictionary<string, LockInfo>();

        public SoodaTransaction Transaction
        {
            get { return _st; }
        }

        public SoodaSession()
        {
            _st = new SoodaTransaction();
        }
        #region INGDataSession Members

        public void Commit()
        {
            _st.Commit();
        }

        public void Rollback()
        {
            _st.Rollback();
        }

        public void Dispose()
        {
            ReleaseAllLocks();
            _st.Dispose();
        }

        internal void ReleaseAllLocks()
        {
            foreach (LockInfo lck in _sessionLocks.Values)
            {
                if (lck.TheLock != null) lck.TheLock.Release();
            }
            _sessionLocks = new Dictionary<string, LockInfo>();
        }

        internal void AddLock(LockInfo li) {
            _sessionLocks.Add(li.InstanceId, li);
        }

        internal LockInfo GetLock(string instanceId)
        {
            LockInfo li;
            return _sessionLocks.TryGetValue(instanceId, out li) ? li : null;
        }

        #endregion

        #region INGDataSession Members


        public ISync LockProcessInstance(string instanceId, bool write)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Engine.Dao.TypedQueries;
using Sooda;
using NLog;
using Spring.Caching;

namespace NGinn.Engine.Dao
{ 
    class ProcessInstanceRepository : IProcessInstanceRepository
    {

        private static Logger log = LogManager.GetCurrentClassLogger();
        private ICache _cache;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;

        public ICache Cache
        {
            get { return _cache; }
            set { _cache = value; }
        }

        public ProcessInstanceRepository()
        {
            _cache = new Spring.Caching.NonExpiringCache();
        }

        #region IProcessInstanceRepository Members

        /*public ProcessInstance GetProcessInstance(string instanceId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {

            SoodaSession ss = (SoodaSession) ds;
            ProcessInstanceDbList dbl = ProcessInstanceDb.GetList(ss.Transaction, ProcessInstanceDbField.InstanceId == instanceId);
            if (dbl.Count == 0) return null;
            ProcessInstance pi = (ProcessInstance)SerializationUtil.Deserialize(dbl[0].InstanceData);
            pi.PersistedVersion = dbl[0].RecordVersion;
            return pi;
        }
        */
        
        /// <summary>
        /// Todo: improve concurrency by moving database access outside the lock
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public ProcessInstance GetProcessInstance(string instanceId)
        {
            int version = -1;
            lock (this)
            {
                byte[] data = (byte[])Cache.Get(instanceId);
                if (data == null)
                {
                    _cacheMisses++;
                    using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
                    {
                        ProcessInstanceDbList dbl = ProcessInstanceDb.GetList(st, ProcessInstanceDbField.InstanceId == instanceId);
                        if (dbl.Count == 0) return null;
                        data = dbl[0].InstanceData;
                        version = dbl[0].RecordVersion;
                        Cache.Insert(instanceId, data, TimeSpan.FromMinutes(30.0));
                    }
                }
                else _cacheHits++;
                ProcessInstance pi = (ProcessInstance)SerializationUtil.Deserialize(data);
                if (version >= 0) pi.PersistedVersion = version;
                log.Debug("Process instance cache hits: {0}, misses: {1}", _cacheHits, _cacheMisses);
                return pi;
            }
        }

        public void InsertNewProcessInstance(ProcessInstance pi)
        {
            pi.Passivate();
            lock (this)
            {
                if (GetProcessInstance(pi.InstanceId) != null) throw new ApplicationException("Duplicate instance ID");
                using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
                {
                    ProcessInstanceDb pdb = new ProcessInstanceDb();
                    pdb.InstanceId = pi.InstanceId;
                    pdb.InstanceData = SerializationUtil.Serialize(pi);
                    pdb.Status = ProcessStatus.GetRef((int) pi.Status);
                    pdb.RecordVersion = pi.PersistedVersion;
                    pdb.LastModified = DateTime.Now;
                    st.Commit();
                }
            }
        }

      
        public void UpdateProcessInstance(ProcessInstance pi, NGinn.Engine.Services.Dao.INGDataSession ds)
        {

            SoodaSession ss = (SoodaSession)ds;
            ProcessInstanceDb pdb = ProcessInstanceDb.Load(ss.Transaction, pi.InstanceId);
            pi.Passivate();
            if (pdb.RecordVersion != pi.PersistedVersion)
            {
                log.Error("Warning: process {0}: in-memory record version ({1}) is different from persisted version ({2})", pi.InstanceId, pi.PersistedVersion, pdb.RecordVersion);
            }
            pdb.Status = ProcessStatus.GetRef((int) pi.Status);
            pdb.InstanceData = SerializationUtil.Serialize(pi);
            pdb.RecordVersion = pdb.RecordVersion + 1;
            pdb.LastModified = DateTime.Now;
        }
        
        public void UpdateProcessInstance(ProcessInstance pi)
        {
            using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
            {
                lock (this)
                {
                    ProcessInstanceDb pdb = ProcessInstanceDb.Load(st, pi.InstanceId);
                    pi.Passivate();
                    if (pdb.RecordVersion != pi.PersistedVersion)
                    {
                        log.Error("Warning: process {0}: in-memory record version ({1}) is different from persisted version ({2})", pi.InstanceId, pi.PersistedVersion, pdb.RecordVersion);
                    }
                    pdb.Status = ProcessStatus.GetRef((int)pi.Status);
                    pdb.InstanceData = SerializationUtil.Serialize(pi);
                    pdb.RecordVersion = pdb.RecordVersion + 1;
                    pdb.LastModified = DateTime.Now;
                    st.Commit();
                    Cache.Remove(pi.InstanceId);
                }
            }
        }



        public ProcessInstance InitializeNewProcessInstance(string definitionId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            ProcessInstanceDb pdb = new ProcessInstanceDb(ss.Transaction);
            pdb.DefinitionId = definitionId;
            pdb.RecordVersion = 0;
            pdb.Status = ProcessStatus.Ready;
            pdb.InstanceId = Guid.NewGuid().ToString("N");
            ProcessInstance pi = new ProcessInstance();
            pi.InstanceId = pdb.InstanceId;
            pi.ProcessDefinitionId = definitionId;
            return pi;
        }

        /*

        public void UpdateToken(Token tok, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            TokenDb tdb = TokenDb.Load(ss.Transaction, tok.TokenId);
            tdb.PlaceId = tok.PlaceId;
            tdb.Status = (int)tok.Status;
            tdb.Mode = (int)tok.Mode;
            tdb.ProcessInstance = tok.ProcessInstanceId;
            tdb.RecordVersion = tdb.RecordVersion + 1;
        }

        public Token GetToken(string tokenId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            TokenDbList dbl = TokenDb.GetList(ss.Transaction, TokenDbField.Id == tokenId);
            if (dbl.Count == 0) throw new Exception("Token not found");
            return ToToken(dbl[0]);
        }

        private Token ToToken(TokenDb tdb)
        {
            Token tok = new Token();
            tok.Mode = (TokenMode) tdb.Mode;
            tok.TokenId = tdb.Id;
            tok.ProcessInstanceId = tdb.ProcessInstance;
            tok.Status = (NGinn.Engine.TokenStatus)tdb.Status;
            tok.PlaceId = tdb.PlaceId;
            tok.Dirty = false;
            tok.PersistedVersion = tdb.RecordVersion;
            return tok;
        }

        private void UpdateTokenDb(Token tok, TokenDb tdb)
        {
            tdb.Mode = (int)tok.Mode;
            tdb.ProcessInstance = tok.ProcessInstanceId;
            tdb.PlaceId = tok.PlaceId;
            tdb.Status = (int)tok.Status;
            //if (tok.PersistedVersion != tdb.RecordVersion) throw new Exception(string.Format("Record version mismatch when persisting token {0}", tok.TokenId));
            tdb.RecordVersion = tdb.RecordVersion + 1;
        }

        public IList<Token> GetProcessActiveTokens(string instanceId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            TokenDbList dbl = TokenDb.GetList(ss.Transaction, TokenDbField.ProcessInstance == instanceId && TokenDbField.Status.In((int)TokenStatus.READY, (int)TokenStatus.WAITING, (int)TokenStatus.LOCKED_ENABLED, (int)TokenStatus.LOCKED_ALLOCATED, (int) TokenStatus.CONSUMED, (int) TokenStatus.CANCELLED));
            List<Token> lt = new List<Token>();
            foreach(TokenDb tdb in dbl)
            {
                lt.Add(ToToken(tdb));
            }
            return lt;
        }
        */

        #endregion

        #region IProcessInstanceRepository Members


        public IList<string> SelectProcessesWithReadyTokens()
        {
            List<string> s = new List<string>();
            using (SoodaTransaction st = new SoodaTransaction())
            {
                ProcessInstanceDbList pdbl = ProcessInstanceDb.GetList(ProcessInstanceDbField.Status == ProcessStatus.Ready, new SoodaOrderBy(ProcessInstanceDbField.CreatedDate, SortOrder.Ascending), SoodaSnapshotOptions.NoWriteObjects);
                foreach (ProcessInstanceDb pdb in pdbl)
                {
                    s.Add(pdb.InstanceId);
                }
            }
            return s;
        }

        #endregion

        #region IProcessInstanceRepository Members


        public string GetProcessOutputXml(string instanceId)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IProcessInstanceRepository Members
        public void SetProcessInstanceErrorStatus(string instanceId, string errorInfo)
        {

            using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
            {
                ProcessInstanceDb inst = ProcessInstanceDb.Load(st, instanceId);
                inst.Status = ProcessStatus.Error;
                inst.ErrorInfo = errorInfo;
                st.Commit();
            }
        }


        public void SetProcessInstanceErrorStatus(string instanceId, string errorInfo, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            ProcessInstanceDb inst = ProcessInstanceDb.Load(instanceId);
            inst.Status = ProcessStatus.Error;
            inst.ErrorInfo = errorInfo;
            /*if (inst.RetryCount > 0)
            {
                inst.RetryCount--;
                inst.NextRetry = DateTime.Now.Add(RetryTimes[inst.RetryCount]);
            }
            else
            {
                //retry limit reached
            }
            inst.LastModified = DateTime.Now;
            */
        }

        #endregion

        
    }
}

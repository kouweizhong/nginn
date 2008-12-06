using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Engine.Dao.TypedQueries;
using Sooda;
using NLog;
using Spring.Caching;
using System.IO;
using System.Xml;
using NGinn.Lib.Data;

namespace NGinn.Engine.Dao
{ 

    /// <summary>
    /// Implementation of process instance repository storing processes in a SQL database.
    /// Custom NGinn serialization is used and processes are stored as XML.
    /// </summary>
    class ProcessInstanceXmlRepository : IProcessInstanceRepository
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

        public ProcessInstanceXmlRepository()
        {
            _cache = new Spring.Caching.NonExpiringCache();
        }

        #region IProcessInstanceRepository Members
 
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
                string data = (string)Cache.Get(instanceId);
                if (data == null)
                {
                    _cacheMisses++;
                    using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
                    {
                        ProcessInstanceDbXmlList dbl = ProcessInstanceDbXml.GetList(st, ProcessInstanceDbXmlField.InstanceId == instanceId);
                        if (dbl.Count == 0) return null;
                        data = dbl[0].InstanceData;
                        version = dbl[0].RecordVersion;
                        Cache.Insert(instanceId, data, TimeSpan.FromMinutes(30.0));
                    }
                }
                else _cacheHits++;
                ProcessInstance pi = new ProcessInstance();
                DataObject dob = DataObject.ParseXml(data);
                pi.RestoreState(dob);
                if (version >= 0) pi.PersistedVersion = version;
                log.Debug("Process instance cache hits: {0}, misses: {1}", _cacheHits, _cacheMisses);
                return pi;
            }
        }

        public void InsertNewProcessInstance(ProcessInstance pi)
        {
            pi.Passivate();
            DataObject dob = pi.SaveState();
            lock (this)
            {
                if (GetProcessInstance(pi.InstanceId) != null) throw new ApplicationException("Duplicate instance ID");
                using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
                {
                    ProcessInstanceDbXml pdb = new ProcessInstanceDbXml();
                    pdb.InstanceId = pi.InstanceId;
                    pdb.InstanceData = dob.ToXmlString("ProcessInstance");
                    pdb.Status = ProcessStatus.GetRef((int) pi.Status);
                    pdb.RecordVersion = pi.PersistedVersion;
                    pdb.LastModified = DateTime.Now;
                    st.Commit();
                }
            }
        }

        public void UpdateProcessInstance(ProcessInstance pi)
        {
            using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
            {
                lock (this)
                {
                    ProcessInstanceDbXml pdb = ProcessInstanceDbXml.Load(st, pi.InstanceId);
                    if (pdb.RecordVersion != pi.PersistedVersion)
                    {
                        log.Error("Warning: process {0}: in-memory record version ({1}) is different from persisted version ({2})", pi.InstanceId, pi.PersistedVersion, pdb.RecordVersion);
                    }
                    pi.PersistedVersion += 1;
                    pi.Passivate();
                    DataObject dob = pi.SaveState();
                    pdb.Status = ProcessStatus.GetRef((int)pi.Status);
                    pdb.InstanceData = dob.ToXmlString("ProcessInstance");
                    pdb.RecordVersion = pi.PersistedVersion;
                    pdb.LastModified = DateTime.Now;
                    st.Commit();
                    Cache.Remove(pi.InstanceId);
                }
            }
        }





        public IList<string> SelectProcessesWithReadyTokens()
        {
            List<string> s = new List<string>();
            using (SoodaTransaction st = new SoodaTransaction())
            {
                ProcessInstanceDbXmlList pdbl = ProcessInstanceDbXml.GetList(ProcessInstanceDbXmlField.Status == ProcessStatus.Ready, new SoodaOrderBy(ProcessInstanceDbXmlField.CreatedDate, SortOrder.Ascending), SoodaSnapshotOptions.NoWriteObjects);
                foreach (ProcessInstanceDbXml pdb in pdbl)
                {
                    s.Add(pdb.InstanceId);
                }
            }
            return s;
        }


        public void SetProcessInstanceErrorStatus(string instanceId, string errorInfo)
        {
            using (SoodaTransaction st = new SoodaTransaction(typeof(ProcessInstanceDb).Assembly))
            {
                ProcessInstanceDbXml inst = ProcessInstanceDbXml.Load(st, instanceId);
                inst.Status = ProcessStatus.Error;
                st.Commit();
            }
        }

        public IList<string> FindProcessesByExternalId(string id)
        {
            throw new NotImplementedException();
        }
        #endregion


    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NLog;
using Spring.Caching;
using System.IO;
using System.Xml;
using NGinn.Lib.Data;
using NHibernate;

namespace NGinn.Engine.Dao
{ 

    /// <summary>
    /// Implementation of process instance repository storing processes in a SQL database.
    /// Custom NGinn serialization is used and processes are stored as XML.
    /// </summary>
    public class ProcessInstanceHibernator : IProcessInstanceRepository
    {

        private static Logger log = LogManager.GetCurrentClassLogger();
        private ICache _cache;

        private ISessionFactory _dbSessionFactory;
        public ISessionFactory SessionFactory
        {
            get { return _dbSessionFactory; }
            set { _dbSessionFactory = value; }
        }


        public ProcessInstanceHibernator()
        {
            _cache = new Spring.Caching.NonExpiringCache();
        }

        public ICache Cache
        {
            get { return _cache; }
            set { _cache = value; }
        }

        protected virtual DataObject GetProcessState(string instanceId)
        {
            using (ISession sess = SessionFactory.OpenSession())
            {
                IQuery qq = sess.CreateQuery("select p from ProcessInstanceData as p where p.id = :instId");
                qq.SetString("instId", instanceId);
                IList<ProcessInstanceData> lst = qq.List<ProcessInstanceData>();
                if (lst.Count == 0) return null;
                DataObject dob = DataObject.ParseXml(lst[0].ProcessData);
                return dob;
            }
        }

        

        
        
        #region IProcessInstanceRepository Members
 
        /// <summary>
        /// Todo: improve concurrency by moving database access outside the lock
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public ProcessInstance GetProcessInstance(string instanceId)
        {
            DataObject dob = (DataObject) Cache.Get(instanceId);
            if (dob == null)
            {
                dob = GetProcessState(instanceId);
                if (dob != null)
                    Cache.Insert(instanceId, dob, TimeSpan.FromMinutes(15));
            }
            if (dob == null) return null;
            ProcessInstance pi = new ProcessInstance();
            pi.RestoreState(dob);
            return pi;
        }

        public virtual void InsertNewProcessInstance(ProcessInstance pi)
        {
            pi.Passivate();
            DataObject dob = pi.SaveState();
            lock (this)
            {
                if (GetProcessInstance(pi.InstanceId) != null) throw new ApplicationException("Duplicate instance ID");
                using (ISession ss = SessionFactory.OpenSession())
                {
                    using (ITransaction tr = ss.BeginTransaction())
                    {
                        ProcessInstanceData pid = new ProcessInstanceData();
                        pid.InstanceId = pi.InstanceId;
                        pid.DefinitionId = pi.ProcessDefinitionId;
                        pid.StartDate = pi.StartDate;
                        pid.Status = ss.Load<ProcessInstanceStatus>((int)pi.Status);
                        pid.RecordVersion = pi.PersistedVersion;
                        pid.LastModified = DateTime.Now;
                        pid.ProcessData = dob.ToXmlString("ProcessInstance");
                        ss.Save(pid);
                        tr.Commit();
                    }
                }
            }
        }

        protected virtual void UpdateProcessInstanceData(ProcessInstance pi)
        {
            
            using (ISession ss = SessionFactory.OpenSession())
            {
                using (ITransaction tr = ss.BeginTransaction())
                {
                    ProcessInstanceData pid = ss.Load<ProcessInstanceData>(pi.InstanceId);
                    if (pid.RecordVersion != pi.PersistedVersion)
                    {
                        log.Warn("Process {0}: Persisted version is {1} and memory version is {2}", pi.InstanceId, pid.RecordVersion, pi.PersistedVersion);
                    }
                    pid.RecordVersion += 1;
                    pi.PersistedVersion = pid.RecordVersion;
                    pi.Passivate();
                    DataObject dob = pi.SaveState();
                    pid.ProcessData = dob.ToXmlString("ProcessInstance");
                    pid.Status = ss.Load<ProcessInstanceStatus>((int)pi.Status);
                    pid.LastModified = DateTime.Now;
                    pid.FinishDate = pi.FinishDate;

                    tr.Commit();
                    Cache.Remove(pi.InstanceId);
                }
            }
        }

        public void UpdateProcessInstance(ProcessInstance pi)
        {
            UpdateProcessInstanceData(pi);
            Cache.Remove(pi.InstanceId);
        }





        public IList<string> SelectProcessesWithReadyTokens()
        {
            List<string> s = new List<string>();
            using (ISession sess = SessionFactory.OpenSession())
            {
                IQuery qq = sess.CreateQuery("select p from ProcessInstanceData as p where p.Status = :st").SetMaxResults(100);
                qq.SetInt32("st", ProcessInstanceStatus.Ready);
                foreach (ProcessInstanceData pd in qq.Enumerable<ProcessInstanceData>())
                {
                    s.Add(pd.InstanceId);
                }
            }
            return s;
        }


        public void SetProcessInstanceErrorStatus(string instanceId, string errorInfo)
        {
            using (ISession ss = SessionFactory.OpenSession())
            {
                using (ITransaction tr = ss.BeginTransaction())
                {
                    ProcessInstanceData pid = ss.Load<ProcessInstanceData>(instanceId);
                    pid.Status = ss.Load<ProcessInstanceStatus>(ProcessInstanceStatus.Error);
                    tr.Commit();
                }
            }
        }

        public IList<string> FindProcessesByExternalId(string id)
        {
            throw new NotImplementedException();
        }
        #endregion


    }
}

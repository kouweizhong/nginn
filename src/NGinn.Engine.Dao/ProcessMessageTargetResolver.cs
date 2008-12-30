using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NLog;
using Sooda;
using NGinn.Engine.Dao.TypedQueries;
using NHibernate;

namespace NGinn.Engine.Dao
{
    /// <summary>
    /// Implementation of message correlation ID resolver
    /// </summary>
    public class ProcessMessageTargetResolver : ITaskCorrelationIdResolver
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        private ISessionFactory _fact;

        public ISessionFactory SessionFactory
        {
            get { return _fact; }
            set { _fact = value; }
        }


        #region ITaskCorrelationIdResolver Members

        public void RegisterMapping(string id, string taskCorrelationId)
        {
            string s = GetCorrelationId(id);
            if (s != null && s != taskCorrelationId) throw new Exception("Mapping already registered for id=" + id);
            if (taskCorrelationId.Equals(s))
            {
                log.Info("Mapping {0}->{1} already registered", id, taskCorrelationId);
                return;
            }
            using (ISession ss = SessionFactory.OpenSession())
            {
                MessageCorrelationMapping mm = new MessageCorrelationMapping();
                mm.MessageId = id;
                mm.TaskCorrelationId = taskCorrelationId;
                ss.Save(mm);
                ss.Flush();
            }
        }

        public string GetCorrelationId(string id)
        {
            lock (this)
            {
                string ret;
                if (_cache.TryGetValue(id, out ret)) return ret;
                using (ISession ss = SessionFactory.OpenSession())
                {
                    IQuery qq = ss.CreateQuery("from MessageCorrelationMapping m where m.MessageId = :mid");
                    qq.SetString("mid", id);
                    IList<MessageCorrelationMapping> lst = qq.List<MessageCorrelationMapping>();
                    if (lst.Count == 0) return null;
                    if (lst.Count > 1)
                    {
                        log.Warn("Not unique message id mapping for id={0}", id);
                    }
                    ret = lst[0].TaskCorrelationId;
                    _cache[id] = ret;
                }
                return ret;
            }
        }

        public void RemoveMapping(string id, string taskCorrelationId)
        {
            lock (this)
            {
                _cache.Remove(id);
                using (ISession ss = SessionFactory.OpenSession())
                {
                    using (ITransaction t = ss.BeginTransaction())
                    {
                        IList<MessageCorrelationMapping> lst = ss.CreateQuery("from MessageCorrelationMapping m where m.MessageId = :mid").SetString("mid", id).List<MessageCorrelationMapping>();
                        foreach (MessageCorrelationMapping m in lst)
                        {
                            if (taskCorrelationId != m.TaskCorrelationId) throw new Exception("Task correlation ID does not match current mapping");
                            log.Info("Removing mapping {0}", m.Id);
                            ss.Delete(m);
                        }
                        ss.Flush();
                        t.Commit();
                    }
                }
            }
        }

        #endregion

        #region ITaskCorrelationIdResolver Members


        public void RemoveAllProcessMappings(string processInstanceId)
        {
            using (ISession ss = SessionFactory.OpenSession())
            {
                ss.Delete(string.Format("from MessageCorrelationMapping where TaskCorrelationId like '{0}.%'", processInstanceId));
                ss.Flush();
            }
        }

        #endregion
    }
}

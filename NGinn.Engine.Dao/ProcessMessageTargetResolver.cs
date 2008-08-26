using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NLog;
using Sooda;
using NGinn.Engine.Dao.TypedQueries;

namespace NGinn.Engine.Dao
{
    /// <summary>
    /// Implementation of message correlation ID resolver
    /// </summary>
    public class ProcessMessageTargetResolver : ITaskCorrelationIdResolver
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private Dictionary<string, string> _cache = new Dictionary<string, string>();

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
            using (SoodaTransaction st = new SoodaTransaction(typeof(MessageCorrelationIdMapping).Assembly))
            {
                MessageCorrelationIdMapping m = new MessageCorrelationIdMapping();
                m.MessageId = id;
                m.TaskCorrelationId = taskCorrelationId;
                st.Commit();
            }
        }

        public string GetCorrelationId(string id)
        {
            lock (this)
            {
                string ret;
                if (_cache.TryGetValue(id, out ret)) return ret;
                using (SoodaTransaction st = new SoodaTransaction(typeof(MessageCorrelationIdMapping).Assembly))
                {
                    MessageCorrelationIdMappingList lst = MessageCorrelationIdMapping.GetList(MessageCorrelationIdMappingField.MessageId == id);
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
                using (SoodaTransaction st = new SoodaTransaction(typeof(MessageCorrelationIdMapping).Assembly))
                {
                    MessageCorrelationIdMappingList lst = MessageCorrelationIdMapping.GetList(MessageCorrelationIdMappingField.MessageId == id);
                    foreach (MessageCorrelationIdMapping m in lst)
                    {
                        if (taskCorrelationId != m.TaskCorrelationId) throw new Exception("Task correlation ID does not match current mapping");    
                        log.Info("Removing mapping {0}", m.Id);
                        m.MarkForDelete();
                    }
                    st.Commit();
                }
            }
        }

        #endregion
    }
}

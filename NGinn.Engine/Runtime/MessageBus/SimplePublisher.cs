using System;
using System.Collections;
using System.Text;
using NLog;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using NGinn.Lib.Interfaces.MessageBus;

namespace NGinn.Engine.Runtime.MessageBus
{
    internal class SubscriberInfo
    {
        public MessageHandler Handler;
        public string Id;
        public Type EventType;
        public string EventTopic;
        public bool ExactMatch;
        public string MatchTopic;

        public SubscriberInfo(string id, Type eventType, string eventTopic, MessageHandler handler)
        {
            Id = id;
            Handler = handler;
            EventTopic = eventTopic;
            EventType = eventType;
            ExactMatch = true;
            MatchTopic = EventTopic == null ? "" : EventTopic;
            if (MatchTopic.EndsWith("*"))
            {
                ExactMatch = false;
                MatchTopic = MatchTopic.Substring(0, MatchTopic.Length - 1);
            }
        }

        public bool MatchesTopic(string topic)
        {
            topic = topic == null ? "" : topic;
            if (MatchTopic.Length == 0) return true;
            if (ExactMatch) return MatchTopic.Equals(topic);
            return topic.StartsWith(MatchTopic);
        }
    
    }
    
    ///Prosty publisher do zarzadzania dystrybucja eventow typu 'Message' i dowolnego innego typu
    ///Mozna sie zapisywac na typ zdarzenia, a w przypadku typu 'Message' na nazwe zdarzenia (EventType)
    public class SimplePublisher : MarshalByRefObject, IMessageBus
    {
        private int _counter = 0;
        private Hashtable _idToSubscriberInfo = new Hashtable(); //id->si
        private Hashtable _typeToSubscriberInfo = new Hashtable(); //typ->si
        private static Logger log = LogManager.GetCurrentClassLogger();

        public SimplePublisher()
        { 
        }

        #region IMessageBus Members

        public virtual object Notify(string sender, string topic, object msg, bool async)
        {
            return NotifyInternal(msg, topic, sender);
        }

        public string Subscribe(Type eventType, string eventTopic, MessageHandler handler)
        {
            lock (this)
            {
                string id = (_counter++).ToString();
                SubscriberInfo si = new SubscriberInfo(id, eventType, eventTopic, handler);
                _idToSubscriberInfo[id] = si;
                ArrayList al = (ArrayList)_typeToSubscriberInfo[eventType];
                if (al == null)
                {
                    al = new ArrayList();
                    _typeToSubscriberInfo[eventType] = al;
                }
                al.Add(si);
                return id;
            }
        }

        protected object NotifyInternal(object msg, string topic, string sender)
        {
            object ret = null;
            Type t = msg.GetType();
            while (t != null)
            {
                object tmp;
                bool b = NotifyInternalForType(msg, topic, sender, t, out tmp);
                if (b) ret = tmp;
                t = t.BaseType;
            }
            return ret;
        }

        private bool NotifyInternalForType(object msg, string topic, string sender, Type t, out object retval)
        {
            retval = null;
            ArrayList al;
            lock (this)
            {
                al = (ArrayList) _typeToSubscriberInfo[t];
            }
            if (al == null) return false;
            return NotifyInternalList(msg, topic, sender, al, out retval);
        }

        private bool NotifyInternalList(object msg, string topic, string sender, ArrayList subscribers, out object retval)
        {
            retval = null;
            bool match = false;
            foreach (SubscriberInfo si in subscribers)
            {
                if (si.MatchesTopic(topic))
                {
                    match = true;
                    retval = si.Handler.Invoke(topic, sender, msg);
                }
            }
            return match;
        }

        public void Unsubscribe(string subscriptionId)
        {
            lock (this)
            {
                SubscriberInfo si = (SubscriberInfo) _idToSubscriberInfo[subscriptionId];
                if (si == null) return;
                _idToSubscriberInfo.Remove(subscriptionId);
                ArrayList al = (ArrayList) _typeToSubscriberInfo[si.EventType];
                if (al != null)
                {
                    al.Remove(si);
                }
            }
        }

        #endregion

        public void SubscribeType(Type t)
        {
            foreach (MethodInfo mi in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                object[] attrs = mi.GetCustomAttributes(typeof(MessageBusSubscriberAttribute), true);
                if (attrs.Length > 0)
                {
                    
                    MessageHandler mh = (MessageHandler)System.Delegate.CreateDelegate(typeof(MessageHandler), mi);
                    foreach (MessageBusSubscriberAttribute sa in attrs)
                    {
                        log.Info("Subscribing {0}.{1} for EventType {2} and Topic {3}", t.Name, mi.Name, sa.EventType.Name, sa.EventTopic);
                        this.Subscribe(sa.EventType, sa.EventTopic, mh);
                    }
                }
            }
        }

        public void SubscribeAssembly(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                SubscribeType(t);
            }
        }

    }
}

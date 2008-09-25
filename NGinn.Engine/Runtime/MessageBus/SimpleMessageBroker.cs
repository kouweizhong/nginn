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
    
    /// <summary>
    /// Simple publish-subscribe message broker.
    /// Does not have store and forward mechanism, all messages
    /// are distributed synchronously.
    /// </summary>
    public class SimpleMessageBroker : MarshalByRefObject, IMessageBus
    {
        private int _counter = 0;
        private Hashtable _idToSubscriberInfo = new Hashtable(); //id->si
        private Hashtable _typeToSubscriberInfo = new Hashtable(); //typ->si
        private static Logger log = LogManager.GetCurrentClassLogger();

        private class MsgContext : IMessageContext
        {
            private object _retval;
            private bool _cancelProcessing = false;
            private bool _notified = false;
            private string _sender;
            private string _topic;

            public MsgContext(string sender, string topic)
            {
                _sender = sender; _topic = topic;
            }

            public object Retval
            {
                get { return _retval; }
                set { _retval = value; }
            }

            public bool CancelFurtherProcessing
            {
                get { return _cancelProcessing; }
                set { _cancelProcessing = value; }
            }

            public bool AnyNotified
            {
                get { return _notified; }
                set { _notified = value; }
            }

            public string Sender
            {
                get { return _sender; }
                set { _sender = value; }
            }

            public string Topic
            {
                get { return _topic; }
                set { _topic = value; }
            }
            
        }

        public SimpleMessageBroker()
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
            MsgContext ctx = new MsgContext(sender, topic);
            ctx.CancelFurtherProcessing = false;

            Type t = msg.GetType();
            while (t != null)
            {
                object tmp;
                NotifyInternalForType(msg, t, ctx);
                if (ctx.CancelFurtherProcessing)
                {
                    ret = ctx.Retval;
                    break;
                }
                t = t.BaseType;
            }
            return ret;
        }

        private void NotifyInternalForType(object msg, Type t, MsgContext ctx)
        {
            ArrayList al;
            lock (this)
            {
                al = (ArrayList) _typeToSubscriberInfo[t];
            }
            if (al == null) return;
            NotifyInternalList(msg, al, ctx);
        }
         
        private void NotifyInternalList(object msg, ArrayList subscribers, MsgContext ctx)
        {
            foreach (SubscriberInfo si in subscribers)
            {
                if (si.MatchesTopic(ctx.Topic))
                {
                    if (si.Handler != null)
                    {
                        ctx.AnyNotified = true;
                        si.Handler.Invoke(msg, ctx);
                        if (ctx.CancelFurtherProcessing)
                        {
                            break;
                        }
                    }
                }
            }
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

        public void SubscribeObject(object obj)
        {
            Type t = obj.GetType();
            foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object[] attrs = mi.GetCustomAttributes(typeof(MessageBusSubscriberAttribute), true);
                if (attrs.Length > 0)
                {
                    try
                    {
                        MessageHandler mh = (MessageHandler)System.Delegate.CreateDelegate(typeof(MessageHandler), obj, mi, true);
                        foreach (MessageBusSubscriberAttribute sa in attrs)
                        {
                            log.Info("Subscribing {0}.{1} for EventType {2} and Topic {3}", t.Name, mi.Name, sa.EventType.Name, sa.EventTopic);
                            this.Subscribe(sa.EventType, sa.EventTopic, mh);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to subscribe handler {0}.{1} : {2}", t.Name, mi.Name, ex);
                        throw;
                    }
                }
            }
        }

        public void UnsubscribeObject(object obj)
        {
            foreach (SubscriberInfo si in _idToSubscriberInfo.Values)
            {
                if (si.Handler != null && si.Handler.Target == obj)
                {
                    si.Handler = null;
                }
            }
        }

    }
}

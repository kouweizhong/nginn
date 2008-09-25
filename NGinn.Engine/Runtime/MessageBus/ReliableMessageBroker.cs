using System;
using System.Collections;
using System.Text;
using NLog;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using NGinn.Lib.Interfaces.MessageBus;
using Spring.Context;

namespace NGinn.Engine.Runtime.MessageBus
{
   
    [Serializable]
    internal class MessageWrapper
    {
        public string Sender;
        public string Topic;
        public object Body;

        public override string ToString()
        {
            return string.Format("Message [Topic: {0}, Sender: {1}, Body: {2} ({3})]", Topic, Sender, Body != null ? Body.GetType().Name : "", Body != null ? Body.ToString() : "");
        }
    }

    
    /// <summary>
    /// Message broker with store and forward support for asynchronous messages and
    /// for handling retries
    /// </summary>
    public class ReliableMessageBroker : SimpleMessageBroker, IMessageHandler
    {
        private SQLQueueProcessor _queueProc = null;
        private bool _inited = false;
        private string _queueName;
        private IApplicationContext _ctx;

        private Logger log = LogManager.GetCurrentClassLogger();

        
        public ReliableMessageBroker()
        {
            _queueName = null;
            InitSubscribers();
            _queueProc = new SQLQueueProcessor();
            _queueProc.MessageHandler = this;
        }

        public ReliableMessageBroker(string queueName)
        {
            InitSubscribers();
            _queueProc = new SQLQueueProcessor();
            _queueProc.MessageHandler = this;
            QueueName = queueName;
        }

        public string QueueName
        {
            get { return _queueProc.InputQueue; }
            set { _queueProc.InputQueue = value; }
        }

        public string ConnectionString
        {
            get { return _queueProc.ConnectionString; }
            set { _queueProc.ConnectionString = value; }
        }

        private void InitSubscribers()
        {
            this.Subscribe(typeof(ScheduledMessage), "*", new MessageHandler(HandleScheduledMessage));
            this.Subscribe(typeof(string), "NGinn.Engine.Runtime.MessageBus.ReliableMessageBus.Control", new MessageHandler(HandleControlMessage)); 
        } 

        private void HandleScheduledMessage(object message, IMessageContext ctx)
        {
            ScheduledMessage sm = message as ScheduledMessage;
            if (sm == null) throw new ArgumentException("Expected ScheduledMessage");
            MessageWrapper mw = new MessageWrapper();
            mw.Sender = ctx.Sender;
            mw.Topic = ctx.Topic;
            mw.Body = sm.Body;
            Hashtable ht = new Hashtable();
            ht["label"] = GetMessageLabel(mw);
            ht["deliver_at"] = sm.DeliverAt;
            string msgid = _queueProc.GetInputPort().SendMessage(mw, ht);
            ctx.Retval = msgid;
        }

        private void HandleControlMessage(object message, IMessageContext ctx)
        {
            string msg = (string)message;
            if (msg == "START")
            {
                Start();
            }
            else if (msg == "STOP")
            {
                Stop();
            }
            else throw new Exception("Unknown command: " + msg);
        }

        public void Start()
        {
            log.Info("Starting messagebus async queue processor");
            _queueProc.Start();
        }

        public void Stop()
        {
            log.Info("Stopping messagebus async queue processor");
            _queueProc.Stop();
        }

        private string GetMessageLabel(MessageWrapper mw)
        {
            string s = string.Format("{0}|{1}|{2}", mw.Topic, mw.Sender, mw.Body.ToString());
            if (s.Length > 100) s = s.Substring(0, 100);
            return s;
        }
            
      

        public override object Notify(string sender, string topic, object msg, bool async)
        {
            if (async && _queueProc != null)
            {
                MessageWrapper mw = new MessageWrapper();
                mw.Sender = sender;
                mw.Topic = topic;
                mw.Body = msg;
                Hashtable ht = new Hashtable();
                ht["label"] = GetMessageLabel(mw);
                string msgid = _queueProc.GetInputPort().SendMessage(mw, ht);
                return msgid;
            }
            else return base.Notify(sender, topic, msg, false);
        }

        public void HandleMessage(object msg, IDictionary headers)
        {
            MessageWrapper mw = msg as MessageWrapper;
            if (mw == null) throw new Exception("Message type not supported: " + msg);
            base.Notify(mw.Sender, mw.Topic, mw.Body, false);
        }

        
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Interfaces;
using NLog;
using B = Boo.Lang;

namespace NGinn.Utilities.MessageBus
{
    public abstract class MessageHandlerBase
    {
        private IMessageBus _msgBus;
        public IMessageBus MessageBus
        {
            get { return _msgBus; }
            set { _msgBus = value; }
        }
        public delegate void Action();
        public abstract void Prepare();

        private Type _msgType;
        public Type MessageType
        {
            get { return _msgType; }
            set { _msgType = value; }
        }

        private string _topic;
        public string MessageTopic
        {
            get { return _topic; }
            set { _topic = value; }
        }

        protected void message_type(Type t)
        {
            MessageType = t;
        }

        protected void message_topic(string s)
        {
            MessageTopic = s;
        }

        IMessageContext _msgCtx;
        object _msg;

        public IMessageContext MessageContext
        {
            get { return _msgCtx; }
            set { _msgCtx = value; }
        }

        public object Message
        {
            get { return _msg; }
            set { _msg = value; }
        }

        private Action _handler;
        protected void handler(Action act)
        {
            
            _handler = act;
        }

        public void Initialize()
        {
            Prepare();
            if (MessageType == null) throw new Exception("message_type not set");
            if (MessageTopic == null) throw new Exception("message_topic not set");
            if (_handler == null) throw new Exception("handler not set");
        }

        public void Execute(object msg, IMessageContext ctx)
        {
            Message = msg;
            MessageContext = ctx;
            if (_handler != null) _handler();
        }

        public Logger log = LogManager.GetCurrentClassLogger();

        private B.IQuackFu _ctx;
        public B.IQuackFu Context
        {
            get { return _ctx; }
            set { _ctx = value; }
        }
    }
}

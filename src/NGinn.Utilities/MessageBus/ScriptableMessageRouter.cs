﻿using System;
using System.Collections;
using System.Text;
using Rhino.DSL;
using NLog;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.MessageBus;
using System.Reflection;
using System.Collections.Generic;
using B = Boo.Lang;
using Spring.Context;

namespace NGinn.Utilities.MessageBus
{
    /// <summary>
    /// Message router for message bus that uses boo scripts for processing the messages.
    /// </summary>
    public class ScriptableMessageProcessor : IApplicationContextAware
    {
        private DslFactory _fact = new DslFactory();
        private IMessageBus _msgBus;
        private List<string> _subscriptions = new List<string>();
        private Logger log = LogManager.GetCurrentClassLogger();

        public ScriptableMessageProcessor()
        {
            ScriptableMessageRouterDSLEngine eng = new ScriptableMessageRouterDSLEngine();
            eng.Namespaces = new string[] {
                "System",
                "System.IO",
                "System.Collections",
                "NLog",
                "NGinn.Utilities.MessageBus"
            };
            eng.ReferencedAssemblies = new Assembly[] {
                typeof(MessageHandlerBase).Assembly
            };
            eng.ReferAllLoadedAssemblies = true;
            _fact.Register<MessageHandlerBase>(eng);
        }

        public IMessageBus MessageBus
        {
            get { return _msgBus; }
            set { _msgBus = value; }
        }

        public string BaseDirectory
        {
            get { return _fact.BaseDirectory; }
            set { _fact.BaseDirectory = value; }
        }

        private IDictionary<string, object> _ctx = new Dictionary<string, object>();
        public IDictionary<string, object> Context
        {
            get { return _ctx; }
            set { _ctx = value; }
        }

        

        public void Initialize()
        {
            if (MessageBus == null) throw new Exception("MessageBus");

            MessageHandlerBase[] bb = _fact.CreateAll<MessageHandlerBase>(BaseDirectory, null);
            foreach (string sub in _subscriptions)
            {
                MessageBus.Unsubscribe(sub);
            }
            _subscriptions.Clear();
            foreach (MessageHandlerBase b in bb)
            {
                b.Initialize();
                b.MessageBus = this.MessageBus;
                if (Context != null) b.Context = new MessageRouterContext(_appCtx, Context);
                string sid = MessageBus.Subscribe(b.MessageType, b.MessageTopic, new MessageHandler(b.Execute));
                _subscriptions.Add(sid);
            }
        }


        #region IApplicationContextAware Members
        private IApplicationContext _appCtx;
        public IApplicationContext ApplicationContext
        {
            set 
            { 
                _appCtx = value;
                Initialize();
            }
        }

        #endregion
    }

    class MessageRouterContext : B.IQuackFu
    {
        private IApplicationContext _ctx;
        private IDictionary<string, object> _dic;
        public MessageRouterContext(IApplicationContext ctx, IDictionary<string, object> dic)
        {
            _ctx = ctx;
            _dic = dic;
        }

        #region IQuackFu Members

        public object QuackGet(string name, object[] parameters)
        {
            object v;
            if (_dic.TryGetValue(name, out v)) return v;
            return _ctx.GetObject(name);
        }

        public object QuackInvoke(string name, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object QuackSet(string name, object[] parameters, object value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

using System;
using System.Collections;
using System.Text;
using Rhino.DSL;
using NLog;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.MessageBus;
using System.Reflection;
using System.Collections.Generic;
using B = Boo.Lang;

namespace NGinn.Utilities.MessageBus
{
    /// <summary>
    /// Message router for message bus that uses boo scripts for processing the messages.
    /// </summary>
    public class ScriptableMessageProcessor : Spring.Objects.Factory.IInitializingObject
    {
        private DslFactory _fact = new DslFactory();
        private IMessageBus _msgBus;
        private List<string> _subscriptions = new List<string>();

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
                if (Context != null) b.Context = new NGinn.RippleBoo.QuackWrapper(Context);
                string sid = MessageBus.Subscribe(b.MessageType, b.MessageTopic, new MessageHandler(b.Execute));
            }
        }

        #region IInitializingObject Members

        public void AfterPropertiesSet()
        {
            Initialize();
        }

        #endregion
    }
}

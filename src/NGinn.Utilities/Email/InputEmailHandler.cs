using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.RippleBoo;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Interfaces;

namespace NGinn.Utilities.Email
{
    abstract internal class EmailRulesBase : NGinn.RippleBoo.RuleSetBase
    {
        protected EmailMessageInfo Message;
        protected IMessageBus MessageBus;

        public override void Initialize()
        {
            Message = (EmailMessageInfo)this.Variables.QuackGet("Message", null);
            MessageBus = (IMessageBus)Context.QuackGet("MessageBus", null);
            base.Initialize();
        }
    }

    /// <summary>
    /// Handler for incoming email messages. Uses NGinn.RippleBoo rules for routing and preprocessing
    /// messages.
    /// </summary>
    public class InputEmailHandler
    {
        private RuleRepository _rulez = new RuleRepository();

        public InputEmailHandler()
        {
            _rulez.BaseType = typeof(EmailRulesBase);
        }

        public string BaseDirectory
        {
            get { return _rulez.BaseDirectory; }
            set { _rulez.BaseDirectory = value; }
        }

        public void HandleEmail(EmailMessageInfo emi)
        {
            Dictionary<string, object> vars = new Dictionary<string,object>();
            Dictionary<string, object> ctx = new Dictionary<string,object>();
            vars["Message"] = emi;
            _rulez.EvaluateRules("email_rules.boo", vars, ctx);
        }

    }
}

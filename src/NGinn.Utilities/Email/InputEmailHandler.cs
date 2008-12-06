using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.RippleBoo;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Interfaces;

namespace NGinn.Utilities.Email
{
    public abstract  class EmailRulesBase : NGinn.RippleBoo.RuleSetBase
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
            _rulez.ImportNamespaces.Add("NGinn.Lib.Data");
            _rulez.ImportNamespaces.Add("NGinn.Lib.Interfaces");
            _rulez.ImportNamespaces.Add("NGinn.Utilities.Email");
        }

        public string BaseDirectory
        {
            get { return _rulez.BaseDirectory; }
            set { _rulez.BaseDirectory = value; }
        }

        private string _rulesFile = "email_rules.boo";
        public string RulesFile
        {
            get { return _rulesFile; }
            set { _rulesFile = value; }
        }

        private IDictionary<string, object> _extCtx = new Dictionary<string, object>();

        public IDictionary<string, object> Context
        {
            get { return _extCtx; }
            set { _extCtx = value; }
        }


        public void HandleEmail(EmailMessageInfo emi)
        {
            Dictionary<string, object> vars = new Dictionary<string,object>();
            vars["Message"] = emi;
            _rulez.EvaluateRules(RulesFile, vars, _extCtx);
        }

    }
}

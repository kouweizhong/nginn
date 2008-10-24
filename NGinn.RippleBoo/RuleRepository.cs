using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.DSL;
using NLog;

namespace NGinn.RippleBoo
{
    public class RuleRepository
    {
        private string _baseDir;
        private Logger log = LogManager.GetCurrentClassLogger();

        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }

        private Type _baseType = typeof(RuleSetBase);
        public Type BaseType
        {
            get { return _baseType; }
            set
            {
                if (value == null) throw new ArgumentException();
                if (value != typeof(RuleSetBase) && !value.IsSubclassOf(typeof(RuleSetBase))) throw new ArgumentException("Base type must be a subclass of RuleSetBase");
                _baseType = value;
            }
        }
        private DslFactory _factory = new DslFactory();

        public RuleRepository()
        {
            RippleBooDslEngine eng = new RippleBooDslEngine();
            _factory.Register(typeof(RuleSetBase), eng);
        }

        private bool _inited = false;
        protected void Initialize()
        {
            if (_inited) return;
            _factory.BaseDirectory = BaseDirectory;
            _inited = true;
        }

        protected RuleSetBase GetNewRuleSet(string name)
        {
            Initialize();
            RuleSetBase rb = (RuleSetBase) _factory.Create(BaseType, name);
            rb.Name = name;
            return rb;
        }

        public void EvaluateRules(string ruleset)
        {
            DateTime ds = DateTime.Now;
            RuleSetBase rb = GetNewRuleSet(ruleset);
            rb.Initialize();
            rb.Execute();
            log.Info("Rule evaluation time: {0}", DateTime.Now - ds);
        }
    }
}

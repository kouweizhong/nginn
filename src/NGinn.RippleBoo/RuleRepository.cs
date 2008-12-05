using System;
using System.Collections.Generic;
using System.Text;
using Rhino.DSL;
using NLog;
using System.IO;

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
            
        }

        private List<string> _namespaces = new List<string>();
        public IList<string> ImportNamespaces
        {
            get { return _namespaces; }
        }

        private bool _inited = false;
        protected void Initialize()
        {
            if (_inited) return;
            RippleBooDslEngine eng = new RippleBooDslEngine();
            eng.BaseType = BaseType;
            eng.Namespaces = _namespaces.ToArray();
            _factory.Register(BaseType, eng);
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

        public void EvaluateRules(string ruleset, IDictionary<string, object> variables, IDictionary<string, object> context)
        {
            DateTime ds = DateTime.Now;
            RuleSetBase rb = GetNewRuleSet(ruleset);
            rb.Variables = new QuackWrapper(variables);
            rb.Context = new QuackWrapper(context);
			rb.Initialize();
            rb.Execute();
            log.Info("Rule evaluation time: {0}", DateTime.Now - ds);
            if (rb._gotoRulesFile != null)
            {
                log.Info("Evaluating included ruleset {1}", rb._gotoRulesFile);
                EvaluateRules(rb._gotoRulesFile, variables, context);
            }
        }

        public void SaveRuleGraph(string rulesFile, string ruleset, string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                RuleSetBase rsb = GetNewRuleSet(rulesFile);
                rsb.Initialize();
                rsb.ToGraph(sw, ruleset);
            }
        }

        
    }
}

using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using Boo.Lang;
using Boo.Lang.Compiler.Ast;
using NLog;
using System.Diagnostics;
using System.IO;


namespace NGinn.RippleBoo
{
    /// <summary>
    /// Base class for ruleset implementation
    /// Warning: RuleSet objects should be used in single thread.
    /// If you need multiple threads, create separate RuleSets.
    /// </summary>
    public abstract class RuleSetBase
    {
        public delegate bool Condition();
        public delegate void Action();

        public class Rule
        {
            public string Id;
            public Condition When;
            public Action Then;
            public string TrueGoto;
            public string FalseGoto;
            public string Label;
        }

        public class Ruleset
        {
            public string Name;
            public Rule StartRule;
            public Dictionary<string, Rule> Rules;

            public Ruleset()
            {
                StartRule = new Rule();
                StartRule.Id = "--default--";
                StartRule.When = delegate() { return true; };
                Rules = new Dictionary<string,Rule>();
            }

            public void AddRule(Rule r)
            {
                if (GetRule(r.Id) != null) throw new Exception("Rule already defined: " + Name + "." + r.Id);
                Rules[r.Id] = r;
                if (StartRule.TrueGoto == null) StartRule.TrueGoto = r.Id;
            }

            public Rule GetRule(string name)
            {
                Rule r;
                if (name == null) return null;
                if (Rules.TryGetValue(name, out r)) return r;
                return null;
            }
        }

        protected Dictionary<string, Ruleset> _ruleSets = new Dictionary<string, Ruleset>();
        private string _defaultRuleset = null;
        
        
        private IQuackFu _variables = new QuackWrapper(new Hashtable());
        private IQuackFu _ctx = new QuackWrapper(new Hashtable());
        private Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Variables container
        /// </summary>
        public IQuackFu Variables
        {
            get { return _variables; }
            set { _variables = value; }
        }

        public IQuackFu V
        {
            get { return Variables; }
        }

        /// <summary>
        /// Context object
        /// </summary>
        public IQuackFu Context
        {
            get { return _ctx; }
            set { _ctx = value; }
        }

       

        private string _name = "";

        /// <summary>
        /// Ruleset name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set {
                _name = value;
                string ln = string.Format("[RULES].{0}", _name == null ? "[]" : _name);
                _log = LogManager.GetLogger(ln);
            }
        }

        internal string _gotoRuleset = null;
        internal string _gotoRulesFile = null;

        /// <summary>
        /// Ruleset logger
        /// </summary>
        protected Logger log
        {
            get { return _log; }
        }

        public RuleSetBase()
        {
        }

        

        Ruleset _curRuleset;
        public void ruleset(string name, Action act)
        {
            if (name == null || name.Length == 0) throw new ArgumentException("ruleset name cannot be empty");
            if (_curRuleset != null) throw new Exception();
            _curRuleset = new Ruleset();
            _curRuleset.Name = name;
            act();
            if (_ruleSets.ContainsKey(name)) throw new Exception("Ruleset already defined: " + name);
            _ruleSets[name] = _curRuleset;
            if (_defaultRuleset == null) _defaultRuleset = name;
            _curRuleset = null;
        }

        public void default_action(Action act)
        {
            if (_curRuleset == null) throw new Exception();
            _curRuleset.StartRule.Then = act;
        }
        

        [Meta]
        public static Expression when(Expression expression)
        {
            BlockExpression condition = new BlockExpression();
            condition.Body.Add(new ReturnStatement(expression));
            return new MethodInvocationExpression(new ReferenceExpression("condition"),condition);
        }

        private Rule _curRule;
        public void rule(string name, Action body)
        {
            if (_curRule != null) throw new Exception("nested rule not allowed");
            if (_curRuleset == null) throw new Exception("No ruleset");
            _curRule = new Rule();
            _curRule.Id = name;
            body();
            _curRuleset.AddRule(_curRule);
            _curRule = null;
        }

        public void condition(Condition cond)
        {
            if (_curRule == null) throw new Exception("no rule");
            _curRule.When = cond;
        }

        public void action(Action act)
        {
            if (_curRule == null) throw new Exception("no rule");
            _curRule.Then = act;
        }

        public void except_rule(string id)
        {
            if (_curRule == null) throw new Exception("no rule");
            _curRule.TrueGoto = id;
        }

        public void else_rule(string id)
        {
            if (_curRule == null) throw new Exception("no rule");
            _curRule.FalseGoto = id;
        }

        protected void goto_ruleset(string name)
        {
            _gotoRuleset = name;
        }

        protected void goto_file(string name)
        {
            _gotoRulesFile = name;
        }

        protected void label(string name)
        {
            if (_curRule == null) throw new Exception("no rule");
            _curRule.Label = name;
        }

        public void Initialize()
        {
            Prepare();
            AfterPrepare();
        }

        protected void AfterPrepare()
        {
            
        }

        
        /// <summary>
        /// override this method and create rules there.
        /// </summary>
        protected abstract void Prepare();

        public Ruleset GetRuleset(string name)
        {
            Ruleset rs;
            if (name == null) name = _defaultRuleset;
            return _ruleSets.TryGetValue(name, out rs) ? rs : null;
        }
        
        public Rule GetRule(string ruleset, string rule)
        {
            Ruleset rs = GetRuleset(ruleset);
            return rs == null ? null : rs.GetRule(rule);
        }

        
        protected bool Execute(Ruleset rs, Rule r)
        {
            log.Debug("Evaluating {0}.{1}", rs.Name, r.Id);
            if (r.When())
            {
                Rule r1 = rs.GetRule(r.TrueGoto);
                if (r1 != null)
                {
                    log.Debug("Rule {0}.{1} true, checking exception {2}.{3}", rs.Name, r.Id, rs.Name, r1.Id);
                    bool b = Execute(rs, r1);
                    if (b)
                    {
                        log.Debug("Exception {0}.{1} to rule {2}.{3} resulted in action - rule {2}.{3} not fired.", rs.Name, r1.Id, rs.Name, r.Id);
                        return true;
                    }
                }
                log.Debug("Firing rule {0}.{1}", rs.Name,r.Id);
                if (r.Then != null) r.Then();
                return true;
            }
            else
            {
                log.Debug("Rule {0}.{1} is false", rs.Name, r.Id);
                Rule r2 = rs.GetRule(r.FalseGoto);
                if (r2 != null)
                {
                    return Execute(rs, r2);
                }
                return false;
            }
        }

        protected void ExecuteRuleset(Ruleset rs)
        {
            log.Debug("Executing ruleset {0}", rs.Name);
            _gotoRuleset = null;
            _gotoRulesFile = null;
            Execute(rs, rs.StartRule);
            if (_gotoRulesFile == null)
            {
                if (_gotoRuleset != null)
                {
                    log.Debug("Jumping to ruleset: {0}", _gotoRuleset);
                    Execute(_gotoRuleset);
                }
            }
        }

        public void Execute(string rulesetName)
        {
            Ruleset rs = GetRuleset(rulesetName);
            if (rs == null) throw new Exception("Ruleset not defined: " + rulesetName);
            ExecuteRuleset(rs);
        }
        /// <summary>
        /// Execute rules
        /// </summary>
        public void Execute()
        {
            Execute(null);
        }

        public void ToGraph(TextWriter tw)
        {
            foreach (Ruleset rs in _ruleSets.Values)
            {
                ToGraph(rs, tw);
            }
        }

        private void ToGraph(Ruleset rs, TextWriter tw)
        {
            tw.WriteLine("digraph {0} {", rs.Name);
            tw.Write("\"{0}\" [", rs.StartRule.Id);
            tw.WriteLine("];");
            ArrayList rules = new ArrayList();
            rules.Add(rs.StartRule);
            foreach (Rule r in rs.Rules.Values)
            {
                rules.Add(r);
            }

            foreach(Rule r in rules) 
            {
                tw.Write("\"{0}\" [", r.Id);
                tw.WriteLine("];");
            }
            
            foreach (Rule r in rules)
            {
                if (r.TrueGoto != null && r.TrueGoto.Length > 0)
                {
                    tw.Write("\"{0}\" -> \"{1}\" [", r.Id, r.TrueGoto);
                    tw.WriteLine("];");
                }

                if (r.FalseGoto != null && r.FalseGoto.Length > 0)
                {
                    tw.Write("\"{0}\" -> \"{1}\" [", r.Id, r.FalseGoto);
                    tw.WriteLine("];");
                }
            }

            tw.WriteLine("}");
        }
    }
}
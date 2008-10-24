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
        }

        protected Dictionary<string, Rule> _rules = new Dictionary<string, Rule>();
        protected Dictionary<string, string> _ruleParents = new Dictionary<string, string>();



        
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

        public void Ruleset(string name)
        {
            Name = name;
        }

        [Meta]
        public static Expression rule(Expression id, Expression ontrue, Expression onfalse, Expression expression, Expression action)
        {
            BlockExpression condition = new BlockExpression();
            condition.Body.Add(new ReturnStatement(expression));
            return new MethodInvocationExpression(
                new ReferenceExpression("RuleDef"),
                id,
                ontrue,
                onfalse,
                condition,
                action
            );
        }

        /*
        protected void When(Condition condition, Action action)
        {
            conditionsAndActions[condition] = action;
        }
        */

        /// <summary>
        /// Define new rule
        /// </summary>
        /// <param name="id">Rule ID</param>
        /// <param name="ontrue">'true' branch next rule</param>
        /// <param name="onfalse">'false' branch next rule</param>
        /// <param name="condition">rule condition</param>
        /// <param name="action">rule actions</param>
        protected void RuleDef(string id, string ontrue, string onfalse, Condition condition, Action action)
        {
            lock (this)
            {
                if (GetRule(id) != null) throw new Exception("Rule already defined: " + id);
                Rule r = new Rule();
                r.Id = id;
                r.TrueGoto = ontrue;
                r.FalseGoto = onfalse;
                r.When = condition;
                r.Then = action;
                _rules[r.Id] = r;
                if (_rules.Count == 1 && id != "START")
                {
                    RuleDef("START", r.Id, null, delegate() { return true; }, delegate() { });
                }
            }
        }

        public void Initialize()
        {
            Prepare();
            AfterPrepare();
        }

        protected void AfterPrepare()
        {
            _ruleParents = new Dictionary<string, string>();
            foreach (Rule r in _rules.Values)
            {
                if (r.TrueGoto != null && r.TrueGoto.Length > 0)
                {
                    if (!_rules.ContainsKey(r.TrueGoto)) throw new Exception(string.Format("Rule {0} has undefined child rule {1}", r.Id, r.TrueGoto));

                    if (_ruleParents.ContainsKey(r.TrueGoto))
                    {
                        throw new Exception(string.Format("Rule {0} cannot be called from {1} because it is already a child of {2}", r.TrueGoto, r.Id, _ruleParents[r.TrueGoto]));
                    }
                    _ruleParents[r.TrueGoto] = r.Id;
                }
                if (r.FalseGoto != null && r.FalseGoto.Length > 0)
                {
                    if (!_rules.ContainsKey(r.FalseGoto)) throw new Exception(string.Format("Rule {0} has undefined child rule {1}", r.Id, r.FalseGoto));
                    if (_ruleParents.ContainsKey(r.FalseGoto))
                    {
                        throw new Exception(string.Format("Rule {0} cannot be called from {1} because it is already a child of {2}", r.FalseGoto, r.Id, _ruleParents[r.FalseGoto]));
                    }
                    _ruleParents[r.FalseGoto] = r.Id;
                }
            }
        }

        
        /// <summary>
        /// override this method and create rules there.
        /// </summary>
        protected abstract void Prepare();

        
        public Rule GetRule(string name)
        {
            if (name == null || name.Length == 0) return null;
            Rule r;
            if (!_rules.TryGetValue(name, out r)) return null;
            return r;
        }

        public string GetParentRuleId(string id)
        {
            string s;
            if (!_ruleParents.TryGetValue(id, out s)) return null;
            return s;
        }

        private string _lastEvaled = null;

        /// <summary>
        /// Rule that fired last during last execution
        /// </summary>
        public string LastFiredRule
        {
            get { return _lastEvaled; }
        }

        /// <summary>
        /// Execute rules
        /// </summary>
        public void Execute()
        {
            Rule nextRule = GetRule("START");
            Debug.Assert(nextRule != null);
            _lastEvaled = null;
            while (nextRule != null)
            {
                try
                {
                    log.Info("Evaluating {0}", nextRule.Id);
                    if (nextRule.When())
                    {
                        log.Info("Rule {0} evaled to true. Executing actions and going to {1}", nextRule.Id, nextRule.TrueGoto == null ? "<end>" : nextRule.TrueGoto);
                        _lastEvaled = nextRule.Id;
                        nextRule.Then();
                        nextRule = GetRule(nextRule.TrueGoto);
                    }
                    else
                    {
                        log.Info("Rule {0} evaled to false. Going to {1}", nextRule.Id, nextRule.FalseGoto == null ? "<end>" : nextRule.FalseGoto);
                        nextRule = GetRule(nextRule.FalseGoto);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error evaluating rule {0}: {1}", nextRule.Id, ex);
                    throw;
                }
            }
            Debug.Assert(_lastEvaled != null);
            log.Info("Finished evaluation. Last fired rule: {0}", _lastEvaled);
        }
    }
}
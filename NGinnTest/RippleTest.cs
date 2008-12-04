using System;
using System.Collections.Generic;
using System.Text;
using NGinn.RippleBoo;
using Boo.Lang.Interpreter;

namespace NGinnTest
{
    public class TestRuleSet2 : RuleSetBase
    {

        protected override void Prepare()
        {
            Name = "TestRuleSet1";
            
        }
    }

    class RippleTest
    {
        public void Test()
        {
            RuleRepository rr = new RuleRepository();
            rr.BaseDirectory = "c:\\dev\\nginn\\tests";
            rr.ImportNamespaces.Add("NGinn.Lib");
            rr.ImportNamespaces.Add("NGinn.Lib.Schema");
            Dictionary<string, object> vars = new Dictionary<string, object>();
            Dictionary<string, object> ctx = new Dictionary<string, object>();

            for (int i = 0; i < 10; i++)
            {
                if (vars.ContainsKey("Counter")) vars.Remove("Counter");
                vars["Counter"] = i;
                rr.EvaluateRules("myrules.boo", vars, ctx);
            }
        }

        
    }
}

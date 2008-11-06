﻿using System;
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
            RuleDef("R1", "R2", "R3",
                delegate() { return DateTime.Now.Second % 2 == 0; },
                delegate() { log.Info("R1 is alive"); });
            RuleDef("R2", "$8", null,
                delegate() { return DateTime.Now.Millisecond > 500; },
                delegate() { log.Info("R2 is alive"); });
            RuleDef("R3", null, null,
                delegate() { return DateTime.Now.Millisecond % 2 == 0; },
                delegate() { log.Info("R3 is alive"); });
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

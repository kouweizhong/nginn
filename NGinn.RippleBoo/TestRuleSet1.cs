using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinn.RippleBoo
{
    public class TestRuleSet1 : RuleSetBase
    {

        protected override void Prepare()
        {
            Name = "TestRuleSet1";
            RuleDef("R1", null, null,
                delegate() { return DateTime.Now.Second % 2 == 0; },
                delegate() { log.Info("R1 is alive"); });
        }
    }
}

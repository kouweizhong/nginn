Ruleset "MyRules"

v = "temp variable"

 

RuleDef "R1", "R2", null, {return 1 == 1}: 
    log.Info("AAA");
    v = "R1 rulez"

RuleDef "R2", "R3", null, {return 1 == 1}:
    log.Info ("AAA: {0}", v)
    
RuleDef "R3", "R4", null, {return 1 == 1}:
    log.Info ("AAA")

RuleDef "R4", null, "R5", {return 1 == 2}:
    log.Info ("AAA")
    
RuleDef "R5", "R6", null, {return 1 == 1}:
    log.Info ("AAA")

rule "R6", "X", null, 2 % 2 == 0, {
    log.Info ("Rul szosty: {0}", date.Now)
}
    
    

rule "X", null, null, date.Today > date.Parse('2008-10-11'):
    log.Warn("The X Rule!!!")


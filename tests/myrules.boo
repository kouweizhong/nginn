Ruleset "MyRules"


rule "R1", "R2", null, 1 == 1: 
    log.Info("AAA");

rule "R2", "R3", null, 1 == 1:
    log.Info ("R2")
    
rule "R3", "R4", null, 1 == 1:
    log.Info ("R3")

rule "R4", null, "R5", 1 == 2:
    log.Info ("R4")
    
rule "R5", "R6", null, 1 == 1:
    log.Info ("R5")

rule "R6", "X", null, 2 % 2 == 0:
    log.Info ("Rule six: {0}", date.Now)
    
rule "X", null, null, date.Today > date.Parse('2008-10-11'):
    log.Warn("The X Rule!!!")


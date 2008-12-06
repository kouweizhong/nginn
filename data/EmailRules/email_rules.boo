ruleset "email_rules":

    rule "1":
        when true
        action:
            log.Info("No to message!!!")
##############################################################################################################
##########################################        ############################################################
#########################################  EMAILS  ###########################################################
##########################################        ############################################################
##############################################################################################################

ruleset "email_rules":
	on_finished:
		pass
		
	default_action:
		pass

	rule "R1":
		label "Nowa sprawa MNP Port Out?"
		when Message.Subject.IndexOf("MNPPortOut") >= 0 and Message.Attachments.Count > 0
		action:
			log.Info("Starting process MNPPortOut.4")
			dob = DataObject.LoadXmlFile(Message.Attachments[0].FileName)
			instid = Context.NGEnvironment.StartProcessInstance("Demo.MNPPortOut.4", dob, Message.From, null)
			log.Info("Process started: {0}", instid)
		else_rule "R2"
	
	rule "R2":
		label "Odpowiedü na mail?"
		when Message.Subject =~ /##(\w+)##/
		action:
			m = /##(\w+)##/.Match(Message.Subject)
			corrid = m.Groups[1].Value
			log.Info("Mail message correlation Id: {0}", corrid)
			dob = DataObject()
			dob["From"] = Message.From
			dob["To"] = Message.To[0]
			dob["Subject"] = Message.Subject
			dob["BodyPlainText"] = Message.BodyPlainText
			dob["Body"] = Message.BodyText
			Context.NGEnvironment.DispatchProcessMessage(corrid, dob)
		else_rule "sms2email"
			
	rule "sms2email":
		label "SMS z bramki"
		when Message.From.EndsWith("sms2email.pl")
		action:
			log.Info("Mail z bramki: {0}", Message.From)
			m = /(48\d+)@/.Match(Message.From)
			raise 'Invalid message sender' unless m.Success
			msisdn = m.Groups[1].Value
			body = Message.BodyPlainText
			m = /---\n([\w\s]+)\n---/.Match(Message.BodyPlainText)
			if m.Success:
				body = m.Groups[1].Value.Trim()
			log.Info("Body: {0}", body)
			dob = DataObject()
			dob["MSISDN"] = msisdn
			dob["Response"] = body
			Context.NGEnvironment.DispatchProcessMessage("Example.1.${msisdn}", dob)
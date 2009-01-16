
message_type NGinn.Utilities.Email.EmailMsgOut
message_topic '*'

handler:
	log.Debug("Sending email message: {0}", Message)
	Context.EmailSender.SendMessage(Message)
	log.Debug("Message sent")
	tn = NGinn.Engine.Runtime.TaskCompletedNotification()
	tn.CorrelationId = Message.CorrelationId
	tn.ProcessInstanceId = NGinn.Engine.ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(Message.CorrelationId)
	tn.TaskData = NGinn.Lib.Data.DataObject()
	MessageBus.NotifyAsync("send_email", "TaskCompleted", tn)

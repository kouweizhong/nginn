
#handler for incoming email messages
message_type NGinn.Utilities.Email.EmailMessageInfo
message_topic '*'

handler:
    log.Info('Nowa wiadomość: ' + Message.Subject)
    Context.EmailHandler.HandleEmail(Message)    

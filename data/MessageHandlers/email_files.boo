
message_type NGinn.Utilities.Email.IncomingEmailFileEvent
message_topic 'IncomingEmailFile*'

handler:
    log.Info('Nowa wiadomość: ' + Message.FileName)
    dec = NGinn.Utilities.Email.MimeEmailDecoder()
    mInfo = dec.ReadMessageFile(Message.FileName)
    log.Info('Od: {0}, Do: {1}, Temat: {2}', mInfo.From, mInfo.To[0], mInfo.Subject)
    log.Info('Treść: {0}', mInfo.BodyPlainText)

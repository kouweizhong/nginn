using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Utilities;
using NGinn.Utilities.Email;
using System.IO;
using NLog;

namespace NGinnServicesHost
{
    class MailFileProcessor : DirectoryInputProcessor
    {
        protected Logger log = LogManager.GetCurrentClassLogger();

        protected override void FileHandler(string fileName)
        {
            RebexMailDecoder med = new RebexMailDecoder();
            log.Debug("Message file found: {0}. Decoding.", fileName);
            string attDir = Path.Combine(BaseDirectory, Path.GetFileNameWithoutExtension(fileName));
            try
            {
                EmailMessageInfo emi = med.DecodeMessageFile(fileName, attDir);
                emi.Channel = this.Name;
                log.Info("Decoded message: {0}", emi);
                MessageBus.Notify("EmailFetcher." + Name, "IncomingEmail", emi, false);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(attDir)) Directory.Delete(attDir, true);
                }
                catch (Exception ex)
                {
                    log.Warn("Failed to delete message attachments: {0}: {1}", fileName, ex);
                }
            }
                
        }
    }
}

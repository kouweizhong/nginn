using System;
using System.Collections.Generic;
using System.Text;
using Rebex.Mime;
using Rebex.Mail;
using NGinn.Utilities.Email;
using System.IO;

namespace NGinnServicesHost
{
    public class RebexMailDecoder
    {

        public EmailMessageInfo DecodeMessageFile(string fileName, string attachmentDir)
        {
            MailMessage mm = new MailMessage();
            mm.Load(fileName);
            
            EmailMessageInfo emi = new EmailMessageInfo();
            emi.MessageId = mm.MessageId.Id;
            emi.From = mm.From[0].Address;
            emi.To = new string[mm.To.Count];
            for (int i = 0; i < mm.To.Count; i++)
            {
                emi.To[i] = mm.To[i].Address;
            }
            emi.Subject = mm.Subject;
            emi.Cc = new string[mm.CC.Count];
            for (int i = 0; i < mm.CC.Count; i++)
            {
                emi.Cc[i] = mm.CC[i].Address;
            }

            emi.BodyPlainText = mm.BodyText;
            emi.BodyText = mm.BodyHtml;
            for (int i = 0; i < mm.Attachments.Count; i++)
            {
                Attachment att = mm.Attachments[i];
                AttachmentInfo ai = new AttachmentInfo();
                ai.Name = att.FileName;
                if (!Directory.Exists(attachmentDir)) Directory.CreateDirectory(attachmentDir);
                string fn = Path.Combine(attachmentDir, att.FileName);
                att.Save(fn);
                ai.FileName = fn;
                emi.Attachments.Add(ai);
            }
            foreach (MimeHeader mh in mm.Headers)
            {
                if (!mh.Unparsable && !emi.Headers.ContainsKey(mh.Name)) 
                    emi.Headers[mh.Name] = mh.Value.ToString();
            }
            return emi;
            
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using anmar.SharpMimeTools;

namespace NGinn.Utilities.Email
{
    /// <summary>
    /// Parse MIME email file and retrieve message information.
    /// </summary>
    public class MimeEmailDecoder
    {
        public EmailMessageInfo ReadMessageFile(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                return ReadMessageStream(fs);
            }
        }

        public EmailMessageInfo ReadMessageStream(Stream stm)
        {
            SharpMessage sm = new SharpMessage(stm);
            
            EmailMessageInfo emi = new EmailMessageInfo();
            emi.From = sm.FromAddress;
            emi.Subject = sm.Subject;
            emi.BodyPlainText = sm.Body;
            List<string> lst = new List<string>();
            foreach (SharpMimeAddress addr in sm.To)
            {
                lst.Add(addr.ToString());
            }
            emi.To = lst.ToArray();
            lst = new List<string>();
            foreach (SharpMimeAddress addr in sm.Cc)
                lst.Add(addr.ToString());
            emi.Cc = lst.ToArray();
            emi.BodyText = sm.Body;
            foreach (SharpAttachment att in sm.Attachments)
            {
            }
            
            foreach (object hdr in sm.Headers)
            {
                        
            }
            return emi;
        }
    }
}

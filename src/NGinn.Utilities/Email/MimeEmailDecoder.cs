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
        public EmailMessageInfo ReadMessageFile(string fileName, string attachmentDir)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                if (attachmentDir == null)
                {
                    string fname = Path.GetFileNameWithoutExtension(fileName);
                    attachmentDir = Path.Combine(Path.GetDirectoryName(fileName), fname);
                }
                return ReadMessageStream(fs, attachmentDir);
            }
        }

        public EmailMessageInfo ReadMessageStream(Stream stm, string attachmentDir)
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
                if (!Directory.Exists(attachmentDir)) Directory.CreateDirectory(attachmentDir);
                AttachmentInfo ai = new AttachmentInfo();
                ai.Name = att.Name;
                string saveFile = Path.Combine(attachmentDir, att.Name);
                att.Save(saveFile, true);
                ai.FileName = saveFile;
                emi.Attachments.Add(ai);
            }
            
            foreach (object hdr in sm.Headers)
            {
            }
            return emi;
        }
    }
}

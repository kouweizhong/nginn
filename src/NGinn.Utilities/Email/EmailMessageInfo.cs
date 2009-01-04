using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Utilities.Email
{
    [Serializable]
    public class AttachmentInfo
    {
        public string Name;
        public string FileName;
    }

    [Serializable]
    public class EmailMessageInfo
    {
        public string Channel;
        public string MessageId;
        public string From;
        public string[] To;
        public string[] Cc;
        public string Subject;
        public string BodyPlainText;
        public string BodyText;
        public List<AttachmentInfo> Attachments = new List<AttachmentInfo>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Message {0} from: {1}, to: {2}, channel: {3}\n", MessageId, From, To.Length > 0 ? To[0] : "", Channel);
            sb.AppendFormat("Subject: {0}\n", Subject);
            foreach (AttachmentInfo ai in Attachments)
            {
                sb.AppendFormat("Attachment {0} ({1})\n", ai.Name, ai.FileName);
            }
            return sb.ToString();
        }
    }
}

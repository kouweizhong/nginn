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
        public string From;
        public string[] To;
        public string[] Cc;
        public string Subject;
        public string BodyPlainText;
        public string BodyText;
        public List<AttachmentInfo> Attachments = new List<AttachmentInfo>();
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
    }
}

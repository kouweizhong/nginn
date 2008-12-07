using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Utilities.Email
{
    [Serializable]
    public class IncomingEmailFileEvent
    {
        public string FileName;
        public string Channel;
        public bool DeleteAfterProcessing = false;
    }
}

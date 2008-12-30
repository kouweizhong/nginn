using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Dao
{
    public class MessageCorrelationMapping
    {
        private int _id;
        public virtual int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _taskCorrId;
        public virtual string TaskCorrelationId
        {
            get { return _taskCorrId; }
            set { _taskCorrId = value; }
        }

        private string _messageId;
        public virtual string MessageId
        {
            get { return _messageId; }
            set { _messageId = value; }
        }
    }
}

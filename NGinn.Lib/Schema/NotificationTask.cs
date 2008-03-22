using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Notification task. Sends an email or SMS notification.
    /// </summary>
    [Serializable]
    public class NotificationTask : Task
    {
        public string _notificationTemplate;

        public override bool IsImmediate
        {
            get { return true; }
        }
    }
}

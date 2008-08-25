using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Engine.Runtime.Email;
using Spring.Context.Support;

namespace NGinn.Engine.Runtime.Tasks
{
    

    /// <summary>
    /// Email notification task
    /// </summary>
    [Serializable]
    public class NotificationTaskActive : ActiveTaskBase
    {
        public NotificationTaskActive(NotificationTask tsk)
            : base(tsk)
        {

        }

        private string _recipients;
        private string _subject;
        private string _body;
        private string _senderName = "EmailSender";

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = false)]
        public string SenderName
        {
            get { return _senderName; }
            set { _senderName = value; }
        }

        [TaskParameter(IsInput=true, Required=true, DynamicAllowed=true)]
        public string Recipients
        {
            get { return _recipients; }
            set { _recipients = value; }
        }

        [TaskParameter(IsInput = true, Required = true, DynamicAllowed = true)]
        public string Subject
        {
            get { return _subject; }
            set { _subject = value; }
        }

        [TaskParameter(IsInput = true, Required = true, DynamicAllowed = true)]
        public string Body
        {
            get { return _body; }
            set { _body = value; }
        }


        public override void CancelTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoInitiateTask()
        {
            EmailMsgOut msg = new EmailMsgOut();
            msg.Recipients = this.Recipients;
            msg.Subject = this.Subject;
            msg.Body = this.Body;
            SMTPEmailSender sender;
            Spring.Context.IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            sender = (SMTPEmailSender) ctx.GetObject(SenderName);
            sender.SendMessage(msg);
            OnTaskCompleted();
        }

        

        
    }
}

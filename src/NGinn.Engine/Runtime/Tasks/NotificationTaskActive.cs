using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Utilities.Email;
using NGinn.Lib;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Data;

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

        private bool _async = true;
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public bool Async
        {
            get { return _async; }
            set { _async = value; }
        }

       

        public override void CancelTask()
        {
            if (_msgid != null)
            {
                Context.EnvironmentContext.MessageBus.CancelAsyncMessage(_msgid);
            }
        }

        private string _msgid = null;

        protected override void DoInitiateTask()
        {
            EmailMsgOut msg = new EmailMsgOut();
            msg.CorrelationId = this.CorrelationId;
            msg.Recipients = this.Recipients;
            msg.Subject = this.Subject;
            msg.Body = this.Body;
            //SMTPEmailSender sender;
            //Spring.Context.IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            //sender = (SMTPEmailSender) ctx.GetObject(SenderName);
            //sender.SendMessage(msg);
            //OnTaskCompleted();
            _msgid = Context.EnvironmentContext.MessageBus.NotifyAsync("NotificationTaskActive." + CorrelationId, "SendEmail." + this.SenderName, msg);
            log.Debug("Message sent: {0}", _msgid);
        }

        public override bool HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            return base.HandleInternalTransitionEvent(ite);
        }

        public override NGinn.Lib.Data.DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            if (_msgid != null) dob["msgid"] = _msgid;
            return dob;
        }

        public override void RestoreState(DataObject dob)
        {
            base.RestoreState(dob);
            dob.TryGet("msgid", ref _msgid);
        }
        
    }
}

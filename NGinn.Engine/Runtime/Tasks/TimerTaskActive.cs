using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;

namespace NGinn.Engine.Runtime.Tasks
{
    [Serializable]
    public class TimerTaskActive : ActiveTaskBase
    {
        private DateTime _expirationTime;
        private TimeSpan _delayAmount;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public TimerTaskActive(Task tsk)
            : base(tsk)
        {

        }

        [TaskParameter(IsInput=true, Required=false, DynamicAllowed=true)]
        public TimeSpan DelayAmount
        {
            get { return _delayAmount; }
            set { _delayAmount = value; }
        }

        

        [TaskParameter(IsInput = false, Required = false, DynamicAllowed = true)]
        public DateTime CompletedDate
        {
            get { return _expirationTime; }
        }

        public override bool IsImmediate
        {
            get { return false; }
        }

        protected override void DoInitiateTask()
        {
            _expirationTime = DateTime.Now + DelayAmount;
            TimerExpiredEvent tex = new TimerExpiredEvent();
            tex.CorrelationId = this.CorrelationId;
            tex.ProcessInstanceId = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(this.CorrelationId);
            tex.ExpirationDate = _expirationTime;
            log.Debug("Timer task {0} expiration date: {1}", CorrelationId, tex.ExpirationDate);
            Context.ParentProcess.Environment.MessageBus.Notify("TimerTaskActive", "TimerTaskActive.TimerExpirationEvent." + CorrelationId, new ScheduledMessage(tex, tex.ExpirationDate), false);

        }

        

        public override void CancelTask()
        {
            
        }

        public override void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            base.HandleInternalTransitionEvent(ite);
            if (ite is TimerExpiredEvent)
            {
                TimerExpiredEvent ev = (TimerExpiredEvent)ite;
                if (Context.Status == TransitionStatus.ENABLED ||
                    Context.Status == TransitionStatus.STARTED)
                {
                    OnTaskCompleted();
                }
                else
                {
                    //ignore the event...
                }
            }
        }
    }

    /// <summary>
    /// Timer task expiration event
    /// </summary>
    [Serializable]
    public class TimerExpiredEvent : InternalTransitionEvent
    {
        public DateTime ExpirationDate;
    }
}

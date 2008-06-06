using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;

namespace NGinn.Engine.Runtime.Tasks
{
    [Serializable]
    public class TimerTaskActive : ActiveTransition
    {
        private DateTime _expirationTime;

        public TimerTaskActive(TimerTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {

        }

        public DateTime ExpirationTime
        {
            get { return _expirationTime; }
        }

        public override bool IsImmediate
        {
            get { return false; }
        }

        public override void InitiateTask()
        {
            base.InitiateTask();
            TimerTask tt = (TimerTask) ProcessTask;
            TimeSpan ts = TimeSpan.Parse(tt.DelayAmount);
            TimerExpiredEvent tex = new TimerExpiredEvent();
            tex.CorrelationId = this.CorrelationId;
            tex.ProcessInstanceId = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(this.CorrelationId);
            tex.ExpirationDate = DateTime.Now + ts;
            log.Debug("Timer task {0} expiration date: {1}", CorrelationId, tex.ExpirationDate);
            this._processInstance.Environment.MessageBus.Notify("TimerTaskActive", "TimerTaskActive.TimerExpirationEvent." + CorrelationId, new ScheduledMessage(tex, tex.ExpirationDate), false);
        }

        public override void TaskCompleted()
        {
            base.TaskCompleted();
        }

        public override void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            base.HandleInternalTransitionEvent(ite);
            if (ite is TimerExpiredEvent)
            {
                TimerExpiredEvent ev = (TimerExpiredEvent)ite;
                if (this.Status == TransitionStatus.ENABLED ||
                    this.Status == TransitionStatus.STARTED)
                {
                    //this.Status = TransitionStatus.COMPLETED;
                    _processInstance.NotifyTransitionCompleted(CorrelationId);
                }
                else
                {
                    //ignore the event...
                }
            }
        }
    }

    [Serializable]
    public class TimerExpiredEvent : InternalTransitionEvent
    {
        public DateTime ExpirationDate;
    }
}

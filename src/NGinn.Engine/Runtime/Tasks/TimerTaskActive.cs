using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using NGinn.Lib.Interfaces;

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
        public string DelayAmount
        {
            get { return _delayAmount.ToString(); }
            set { _delayAmount = TimeSpan.Parse(value); }
        }

        

        [TaskParameter(IsInput = false, Required = false, DynamicAllowed = true)]
        public DateTime CompletedDate
        {
            get { return _expirationTime; }
        }

        public string DoTestProperty
        {
            set
            {
                log.Warn("AAAAAA: DOTEST: {0}", value);
            }
        }
        
        protected override void DoInitiateTask()
        {
            _expirationTime = DateTime.Now + _delayAmount;
            TimerExpiredEvent tex = new TimerExpiredEvent();
            tex.CorrelationId = this.CorrelationId;
            tex.ProcessInstanceId = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(this.CorrelationId);
            tex.ExpirationDate = _expirationTime;
            log.Debug("Timer task {0} expiration date: {1}", CorrelationId, tex.ExpirationDate);
            Context.EnvironmentContext.MessageBus.Notify("TimerTaskActive", "TimerTaskActive.TimerExpirationEvent." + CorrelationId, new ScheduledMessage(tex, tex.ExpirationDate), false);

        }

        

        public override void CancelTask()
        {
            log.Info("Timer task {0} cancelling", CorrelationId);
        }

        public override bool HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            base.HandleInternalTransitionEvent(ite);
            if (ite is TimerExpiredEvent)
            {
                TimerExpiredEvent ev = (TimerExpiredEvent)ite;
                if (Context.Status == TransitionStatus.ENABLED ||
                    Context.Status == TransitionStatus.STARTED)
                {
                    OnTaskCompleted();
                    return true;
                }
            }
            return false;
        }

        public override DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            dob["DelayAmount"] = DelayAmount;
            dob["CompletedDate"] = _expirationTime;

            return dob;
        }

        public override void RestoreState(DataObject dob)
        {
            base.RestoreState(dob);
            DelayAmount = (string)dob["DelayAmount"];
            if (dob.ContainsKey("CompletedDate"))
            {
                _expirationTime = (DateTime) Convert.ChangeType(dob["CompletedDate"], typeof(DateTime));
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

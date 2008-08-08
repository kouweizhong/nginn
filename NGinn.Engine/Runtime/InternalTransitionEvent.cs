using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;

namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// Event notification used for dispatching various events to active task instances
    /// </summary>
    [Serializable]
    public class InternalTransitionEvent
    {
        public string ProcessInstanceId;
        public string CorrelationId;
        public DateTime TimeStamp = DateTime.Now;

        public override string ToString()
        {
            return string.Format("InternalEvent: {0}, CorrelationId={1}", GetType().Name, CorrelationId);
        }
    }

    /// <summary>
    /// Event for notifying task that it has been selected
    /// Send this notification to environment's messagebus to notify process
    /// that the transition has been selected for execution
    /// </summary>
    [Serializable]
    public class TransitionSelectedNotification : InternalTransitionEvent
    {
    }

    /// <summary>
    /// Event for notifying task that is has been completed.
    /// Send this event to environment's messagebus to notify process 
    /// that task instance has completed
    /// </summary>
    [Serializable]
    public class TaskCompletedNotification : InternalTransitionEvent
    {
        public string CompletedBy;
        public DataObject TaskData;
    }

}

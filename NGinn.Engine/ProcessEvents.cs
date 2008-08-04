using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine
{
    [Serializable]
    public class ProcessEvent
    {
        public string InstanceId;
        public string DefinitionId;
        public string CorrelationId;
        public DateTime TimeStamp = DateTime.Now;
    }

    [Serializable]
    public class ProcessStarted : ProcessEvent
    {
    }

    [Serializable]
    public class ProcessFinished : ProcessEvent
    {
    }

    [Serializable]
    public class TransitionEvent : ProcessEvent
    {
        public string TaskId;
        public string TaskType;
        public bool MultiInstance = false;
    }

    [Serializable]
    public class ActiveTransitionStarted : TransitionEvent
    {
        public bool Immediate;
        public string[] Tokens;
    }

    [Serializable]
    public class ActiveTransitionCompleted : TransitionEvent
    {
    }

    [Serializable]
    public class ActiveTransitionCancelled : TransitionEvent
    {
    }

}

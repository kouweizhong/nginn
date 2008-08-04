using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Runtime
{
    [Serializable]
    public class InternalTransitionEvent
    {
        public string ProcessInstanceId;
        public string CorrelationId;
    }
}

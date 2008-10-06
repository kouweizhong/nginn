using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class ReceiveMessageTask : Task
    {
       

        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("MessageCorrelationId", typeof(string), false, true, true),
            };
        }
    }
}

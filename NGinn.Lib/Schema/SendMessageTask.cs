using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Sends a message to NGinn message broker.
    /// Message can be picked up by other processes in the same environment
    /// or by some external handlers.
    /// In order for the message to be picked up, some ReceiveMessageTask must 
    /// be waiting for it (with the same MessageCorrelationId)
    /// TODO decide if it is necessary for the message recipient to be already waiting
    /// for the message when it is sent, or if the message can be persisted
    /// so it will be received when someone waits for it...
    /// </summary>
    [Serializable]
    public class SendMessageTask : Task
    {
        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("MessageCorrelationId", typeof(string), false, true, true),
            };
        }
    }
}

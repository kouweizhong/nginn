using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Timer task - pauses for a specified amount of time (timer transition fires after specified amount of time
    /// has passed since the transition has been enabled)
    /// </summary>
    [Serializable]
    public class TimerTask : Task
    {

       

        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("DelayAmount", typeof(TimeSpan), false, true, true),
                new TaskParameterInfo("DelayUntil", typeof(DateTime), false, true, true),
            };
        }

        internal override bool Validate(IList<ValidationMessage> messages)
        {
            bool b = base.Validate(messages);
            if (!b) return b;
            if (!RequireInputParameter("DelayAmount") && !RequireInputParameter("DelayUntil"))
            {
                messages.Add(new ValidationMessage(true, Id, "Either DelayAmount or DelayUntil task parameter is required"));
            }
            return messages.Count == 0 ? true : false;
        }
    }
}

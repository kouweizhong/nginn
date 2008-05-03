using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Timer task - pauses for a specified amount of time (timer transition fires after specified amount of time
    /// has passed since the transition has been enabled)
    /// </summary>
    [Serializable]
    public class TimerTask : Task
    {
        private string _delayAmount;

        /// <summary>
        /// delay time, in TimeSpan format (for example: 3d 11:30 means 3 days, 11 hours and 30 minutes)
        /// </summary>
        public string DelayAmount
        {
            get { return _delayAmount; }
            set { _delayAmount = value; }
        }

        internal override void LoadXml(System.Xml.XmlElement el, System.Xml.XmlNamespaceManager nsmgr)
        {
            base.LoadXml(el, nsmgr);
            string p = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
        }


        public override bool IsImmediate
        {
            get { return false; }
        }
    }
}

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
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            XmlElement tEl = (XmlElement)el.SelectSingleNode(pr + "timerTask", nsmgr);
            if (tEl == null) throw new Exception("Missing <timerTask> element");
            XmlElement tBody = (XmlElement)tEl.SelectSingleNode(pr + "delayTime", nsmgr);
            if (tBody == null) throw new Exception("Missing <delayTime> element");
            DelayAmount = tBody.InnerText;
        }


        public override bool IsImmediate
        {
            get { return false; }
        }

        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("DelayAmount", typeof(TimeSpan), false, true, true),
                new TaskParameterInfo("DelayUntil", typeof(DateTime), false, true, true),
            };
        }
    }
}

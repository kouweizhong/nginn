using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class ScriptTask : Task
    {
        private string _code;

        public string Script
        {
            get { return _code; }
            set { _code = value; }
        }

        internal override void LoadXml(System.Xml.XmlElement el, System.Xml.XmlNamespaceManager nsmgr)
        {
            base.LoadXml(el, nsmgr);
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            XmlElement tEl = (XmlElement) el.SelectSingleNode(pr + "scriptTask", nsmgr);
            if (tEl == null) throw new Exception("Missing <scriptTask> element");
            XmlElement tBody = (XmlElement) tEl.SelectSingleNode(pr + "script", nsmgr);
            if (tBody == null) throw new Exception("Missing <script> element");
            Script = tBody.InnerText;
        }

        public override bool IsImmediate
        {
            get { return true; }
        }
    }
}

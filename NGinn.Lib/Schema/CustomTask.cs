using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Custom task - based on provided task implementation class
    /// </summary>
    [Serializable]
    public class CustomTask : Task
    {

        private string _implementationClass;
        private bool _isImmediate;

        public string ImplementationClass
        {
            get { return _implementationClass; }
            set { _implementationClass = value; }
        }

        public override bool IsImmediate
        {
            get { return _isImmediate; }
        }

        internal override void LoadXml(System.Xml.XmlElement el, System.Xml.XmlNamespaceManager nsmgr)
        {
            base.LoadXml(el, nsmgr);
        }

        internal override bool Validate(IList<ValidationMessage> messages)
        {
            return base.Validate(messages);
            
        }
    }
}

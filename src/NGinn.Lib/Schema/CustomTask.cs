using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Custom task - based on provided task implementation class
    /// We need to handle the case when actual task implementation class
    /// is not known before evaluation of runtime task parameters.
    /// </summary>
    [Serializable]
    public class CustomTask : Task
    {
        private bool _isImmediate;
        

       
        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("ImplementationClass", typeof(string), true, true, true),
            };
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

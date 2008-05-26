using System;
using System.Collections.Generic;
using System.Text;

namespace XmlProc.Elements
{
    [ElementHandler(NamespaceUri=Helper.PreprocessNamespace, ElementName="element")]
    class AnyElementHandler : IElementHandler
    {
        #region IElementHandler Members

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            string elName = ei.GetAttributeValue("name");
            string elNs = ei.GetAttributeValue("namespace");
            if (elNs == null) elNs = Helper.TargetNamespace;
            context.Output.WriteStartElement(elName, elNs);
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            context.Output.WriteEndElement();
        }

        #endregion
    }
}

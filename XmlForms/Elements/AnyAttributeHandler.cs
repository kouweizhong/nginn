using System;
using System.Collections.Generic;
using System.Text;

namespace XmlProc.Elements
{
    [ElementHandler(NamespaceUri=Helper.PreprocessNamespace, ElementName="attribute")]
    class AnyAttributeHandler : IElementHandler
    {
        #region IElementHandler Members

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            string elName = ei.GetAttributeValue("name");
            string attrVal = ei.GetAttributeValue("value");
            object rv = context.Eval(attrVal);
            context.Output.WriteAttributeString(elName, Convert.ToString(rv));
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            
        }

        #endregion
    }
}

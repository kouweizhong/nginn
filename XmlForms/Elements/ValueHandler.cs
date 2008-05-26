using System;
using System.Collections.Generic;
using System.Text;

namespace XmlProc.Elements
{
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="value")]
    class ValueHandler : IElementHandler
    {
        #region IElementHandler Members

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            AttributeInfo tst = ei.GetAttribute("expr");
            if (tst == null) throw new Exception("Missing 'expr' attribute in <value>");
            string expr = tst.Value;
            object obj = context.Eval(expr);
            if (obj != null) context.Output.WriteString(Convert.ToString(obj));
        }

        public void ElementEnd(IFormHandlerContext context)
        {
        }

        #endregion
    }
}

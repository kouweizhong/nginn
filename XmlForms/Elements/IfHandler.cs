using System;
using System.Collections.Generic;
using System.Text;

namespace XmlProc.Elements
{
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="if")]
    class IfHandler : IElementHandler
    {
        #region IElementHandler Members

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            AttributeInfo tst = ei.GetAttribute("test");
            if (tst == null) throw new Exception("Missing 'test' attribute in <if>");
            string expr = tst.Value;
            object obj = context.Eval(expr);
            if (obj == null || !Convert.ToBoolean(obj))
                context.SkipElementContent = true;
            else
                context.SkipElementContent = false;
        }

        public void ElementEnd(IFormHandlerContext context)
        {
        }

        #endregion
    }
}

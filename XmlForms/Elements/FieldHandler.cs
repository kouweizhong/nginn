using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NLog;

namespace XmlProc.Elements
{
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="field")]
    class FieldHandler : IElementHandler
    {
        #region IElementHandler Members
        private DefaultDataBinder _binder;

        private void FindObjectView(IFormHandlerContext ctx)
        {
            ElementInfo ei = null;
            for (int i = ctx.ElementStackSize - 1; i > 0; i--)
            {
                ElementInfo t = ctx.PeekElementStack(i);
                if (t.Handler is ObjectViewHandler)
                {
                    ei = t;
                    break;
                }
            }
            if (ei == null) throw new Exception("Object view not found");
            _binder = ((ObjectViewHandler)ei.Handler).DataBinder;
        }

        public void ElementStart(IFormHandlerContext context)
        {
            FindObjectView(context);
            object root = null; // context.T;
            ElementInfo ei = context.CurrentElement;
            string fname = ei.GetAttributeValue("name");
            FormFieldInfo fi = _binder.GetFieldInfo(fname);
            XmlWriter o = context.Output;
            o.WriteStartElement("field", Helper.TargetNamespace);
            o.WriteAttributeString("mode", fi.Mode.ToString());
            o.WriteAttributeString("value", fi.Value);
            if (fi.DisplayValue != null) o.WriteAttributeString("display", fi.DisplayValue);
            o.WriteAttributeString("name", fi.Name);
            o.WriteAttributeString("data_source", fi.DataSource);
            o.WriteAttributeString("id", context.GetNextId());
            o.WriteEndElement();
            
            
        }

        public void ElementEnd(IFormHandlerContext context)
        {
        }

        #endregion
    }
}

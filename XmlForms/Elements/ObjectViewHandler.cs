using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XmlProc.Elements
{
    

    /// <summary>
    /// r2:objectview
    /// daje w wyniku r:context
    /// 
    /// </summary>
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="objectview")]
    class ObjectViewHandler : IElementHandler
    {
        #region IElementHandler Members
        private object _tmp;
        private object _t;
        private IFormHandlerContext _ctx;
        private DefaultDataBinder _dataBinder;
        private string id;

        public object T
        {
            get { return _t; }
        }
        
        public DefaultDataBinder DataBinder
        {
            get { return _dataBinder; }
        }

        public void ElementStart(IFormHandlerContext context)
        {
            id = context.GetNextId();
            ElementInfo ei = context.CurrentElement;
            string objref = ei.GetAttributeValue("objref");
            object key = null;
            string k = ei.GetAttributeValue("key");
            if (k != null)
            {
                key = context.Eval(k);
                objref = string.Format("{0}*{1}", objref, key);
            }
            object obj = DefaultDataBinder.GetObjectByRef(objref);
            context.Root = obj;
            _t = obj;
            _dataBinder = new DefaultDataBinder(_t);
            _tmp = context.Root;
            context.Root = obj;
            
            context.Output.WriteStartElement("context", "http://www.rg.com");
            context.Output.WriteAttributeString("objref", objref);
            context.Output.WriteAttributeString("key", Convert.ToString(key));
            context.Output.WriteAttributeString("id", id);
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            context.Output.WriteEndElement();
            context.Root = _tmp;
        }

        #endregion
    }
}

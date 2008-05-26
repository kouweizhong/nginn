using System;
using System.Collections.Generic;
using System.Text;
using NLog;


namespace XmlProc.Elements
{
    /*
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="soodatransaction")]
    class SoodaTransactionHandler : IElementHandler
    {
        #region IElementHandler Members
        private SoodaTransaction _tran;
        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            AttributeInfo tst = ei.GetAttribute("test");
            _tran = new SoodaTransaction();
            //string xml = (string) context.ViewState["sooda-transaction"];
            //if (xml != null) _tran.Deserialize(xml);
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            context.Output.WriteStartElement("transaction", "http://www.rg.com");
            context.Output.WriteAttributeString("modifications", _tran.DirtyObjects.Count.ToString());
            context.Output.WriteEndElement();
            //context.ViewState["sooda-transaction"] = _tran.Serialize();
            _tran.Rollback();
            _tran.Dispose();
        }

        #endregion
    }
     */
}

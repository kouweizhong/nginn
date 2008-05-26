using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NLog;

namespace XmlProc.Elements
{
    /// <summary>
    /// d¿efko
    /// moze miec wiele 'rootów'
    /// dane - pochodz¹ ze struktury hierarchicznej
    /// </summary>
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="tree")]
    class TreeHandler : IElementHandler
    {
        #region IElementHandler Members
        private DefaultDataBinder _binder;

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            context.Output.WriteStartElement("tree", Helper.TargetNamespace);
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            context.Output.WriteEndElement();
        }

        #endregion
    }
}

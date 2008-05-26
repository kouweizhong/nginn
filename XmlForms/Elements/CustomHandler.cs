using System;
using System.Collections.Generic;
using System.Text;

namespace XmlProc.Elements
{
    /// <summary>
    /// Handler 'custom' pozwalaj¹cy podaæ typ handlera który rzeczywiœcie zostanie u¿yty
    /// Sens? ma taki, ¿e typ mo¿e byæ wyznaczony dynamicznie (na razie nie wiem jeszcze jak)
    /// </summary>
    [ElementHandler(NamespaceUri=Helper.PreprocessNamespace, ElementName="custom")]
    class CustomHandler : IElementHandler
    {
        #region IElementHandler Members
        private IElementHandler _handler;

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            string typeName = ei.GetAttributeValue("type");
            if (typeName == null) throw new Exception("type not specified in <custom>");
            Type t = Type.GetType(typeName);
            if (t == null) throw new Exception("Type not found: " + typeName);
            _handler = (IElementHandler) Activator.CreateInstance(t);
            if (_handler == null) throw new Exception("Failed to create instance of type " + typeName);
            _handler.ElementStart(context);
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            _handler.ElementEnd(context);
        }

        #endregion
    }
}

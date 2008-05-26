using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XmlProc.Elements
{
    [ElementHandler(NamespaceUri="http://www.rg.com/preprocess", ElementName="webpage")]
    [ElementHandler(NamespaceUri = "http://www.rg.com/preprocess", ElementName = "webform")]
    class WebFormHandler : IElementHandler
    {
        //private static PersistenceManager _pm = new PersistenceManager();

        #region IElementHandler Members
        private string tid;
        private string mode;

        public void ElementStart(IFormHandlerContext context)
        {
            ElementInfo ei = context.CurrentElement;
            tid = ei.GetAttributeValue("tid");
            if (tid != null) tid = Convert.ToString(context.Eval(tid));
            mode = ei.GetAttributeValue("persist-mode");
            if (mode == null) mode = "savepoint";
            string xsl = ei.GetAttributeValue("stylesheet");
            if (xsl != "none")
            {
                if (xsl == null) xsl = "szablon.xsl";
                context.Output.WriteProcessingInstruction("xml-stylesheet", string.Format("type=\"text/xsl\" href=\"{0}\"", xsl));
            }
            Dictionary<string, object> state;
            //if (tid != null)
            //{
                //context.ViewState = _pm.GetState(tid);
            //}
            //if (context.ViewState == null)
            //{
                //context.ViewState = new Dictionary<string, object>();
            //}
            string elName = ei.Name;
            context.Output.WriteStartElement("r", elName, "http://www.rg.com");
            foreach (AttributeInfo ai in ei.Attributes.Values)
            {
                if (ai.Name == "tid") continue;
                context.Output.WriteAttributeString(ai.Prefix, ai.Name, ai.NamespaceUri, ai.Value);
            }
        }

        public void ElementEnd(IFormHandlerContext context)
        {
            string newTid = null;
            bool save = true;
            if (mode == "discard")
            {
                save = false;
            }
            else if (mode == "savepoint")
            {
                //newTid = _pm.AllocateSubTid(tid);
            }
            else throw new Exception("Unexpected persist-mode. supported are: discard, savepoint, commit");
            if (save)
            {
                //_pm.SaveState(context.ViewState, newTid);
            }
            context.Output.WriteStartElement("pagecontext", "http://www.rg.com");
            context.Output.WriteAttributeString("tid", newTid);
            context.Output.WriteEndElement();
            context.Output.WriteEndElement();
        }

        #endregion
    }
}

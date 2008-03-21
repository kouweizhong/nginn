using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Spring.Core.IO;
using Spring.Context;

namespace NGinn.Lib.Schema
{
    internal class SchemaUtil
    {
        public static string GetXmlElementText(XmlElement parent, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNode t = parent.SelectSingleNode(xpath, nsmgr);
            if (t == null) return null;
            if (t is XmlElement)
                return t.InnerText;
            else
                return t.Value;
        }

        public static XmlReader GetWorkflowSchemaReader()
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            IResource rc = ctx.GetResource("assembly://NGinn.Lib/NGinn.Lib/WorkflowDefinition.xsd");
            return XmlReader.Create(rc.InputStream);
        }
    }
}

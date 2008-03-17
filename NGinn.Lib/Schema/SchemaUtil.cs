using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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
    }
}

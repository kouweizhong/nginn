using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinn.Lib.Schema;

namespace NGinn.Lib.Data
{
    /// <summary>
    /// Enumeration type definition
    /// </summary>
    [Serializable]
    public class EnumDef : TypeDef
    {
        private SimpleTypeDef _baseType = TypeSet.TYPE_STRING;
        private List<object> _values = new List<object>();

        public IList<object> EnumValues
        {
            get { return _values; }
        }

        public SimpleTypeDef BaseType
        {
            get { return _baseType; }
        }

        public override bool IsSimpleType
        {
            get { return false; }
        }

        public override void WriteXmlSchemaType(System.Xml.XmlWriter xw)
        {
            xw.WriteStartElement("simpleType", SchemaUtil.SCHEMA_NS);
            if (Name != null) xw.WriteAttributeString("name", Name);
            xw.WriteStartElement("restriction", SchemaUtil.SCHEMA_NS);
            xw.WriteAttributeString("base", "xs:" + BaseType.Name);
            foreach(object val in this.EnumValues)
            {
                xw.WriteStartElement("enumeration", SchemaUtil.SCHEMA_NS);
                xw.WriteAttributeString("value", val.ToString());
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteEndElement();

        }

        public void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            Name = el.GetAttribute("name");
            foreach(XmlElement v in el.SelectNodes(pr + "value", nsmgr))
            {
                string sv = v.InnerText;
                object ev = Convert.ChangeType(sv, _baseType.ValueType);
                _values.Add(ev);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinn.Lib.Schema;

namespace NGinn.Lib.Data
{
    [Serializable]
    public class MemberDef
    {
        private string _name;
        private string _typeName;
        private bool _isArray;
        private bool _isRequired;

        public MemberDef()
        {
        }

        public MemberDef(MemberDef rhs) : this(rhs.Name, rhs.TypeName, rhs.IsArray, rhs.IsRequired)
        {
        }

        public MemberDef(string name, string typeName, bool isArray, bool isRequired)
        {
            _name = name; _typeName = typeName; _isArray = isArray; _isRequired = isRequired;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        public bool IsArray
        {
            get { return _isArray; }
            set { _isArray = value; }
        }

        public bool IsRequired
        {
            get { return _isRequired; }
            set { _isRequired = value; }
        }

        public virtual void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            Name = el.GetAttribute("name");
            TypeName = el.GetAttribute("type");
            string t = el.GetAttribute("isArray");
            IsArray = "true".Equals(t) || "1".Equals(t);
            t = el.GetAttribute("required");
            IsRequired = t == null || t.Length == 0 || "true".Equals(t) || "1".Equals(t);
        }
    }

    [Serializable]
    public abstract class TypeDef
    {
        private string _name;
        private TypeSet _typeSet;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public abstract bool IsSimpleType
        {
            get;
        }

        public TypeSet ParentTypeSet
        {
            get { return _typeSet; }
            set { _typeSet = value; }
        }

        public virtual void WriteXmlSchemaType(XmlWriter xw)
        {
        }

        
    }

    [Serializable]
    public class StructDef : TypeDef
    {
        private List<MemberDef> _members = new List<MemberDef>();
        public IList<MemberDef> Members
        {
            get { return _members; }
        }

        public override bool IsSimpleType
        {
            get { return false; }
        }

        public override void WriteXmlSchemaType(XmlWriter xw)
        {
            xw.WriteStartElement("complexType", SchemaUtil.SCHEMA_NS);
            if (Name != null) xw.WriteAttributeString("name", Name);
            xw.WriteStartElement("sequence", SchemaUtil.SCHEMA_NS);
            foreach (MemberDef member in Members)
            {
                xw.WriteStartElement("element", SchemaUtil.SCHEMA_NS);
                xw.WriteAttributeString("name", member.Name);
                TypeDef td = ParentTypeSet.GetTypeDef(member.TypeName);
                if (td.IsSimpleType) 
                    xw.WriteAttributeString("type", "xs:" + td.Name);
                else
                    xw.WriteAttributeString("type", td.Name);
                xw.WriteAttributeString("minOccurs", member.IsRequired ? "1" : "0");
                xw.WriteAttributeString("maxOccurs", member.IsArray ? "unbounded" : "1");
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
            foreach(XmlElement mel in el.SelectNodes(pr + "member", nsmgr))
            {
                MemberDef md = new MemberDef();
                md.LoadFromXml(mel, nsmgr);
                _members.Add(md);
            }
        }

        
    }

    [Serializable]
    public class SimpleTypeDef : TypeDef
    {
        private Type _valueType;
        
        public Type ValueType
        {
            get { return _valueType; }
            set { _valueType = value; }
        }

        

        public SimpleTypeDef()
        {
        }
        
        public SimpleTypeDef(string name, Type valueType)
        {
            Name = name;
            ValueType = valueType;
        }

        public override bool IsSimpleType
        {
            get { return true; }
        }
    }

}

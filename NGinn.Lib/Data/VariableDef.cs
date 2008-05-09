using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinn.Lib.Schema;

namespace NGinn.Lib.Data
{
    /// <summary>
    /// Variable definition - used for defining process data schemas
    /// </summary>
    [Serializable]
    public class VariableDef : MemberDef
    {
        public enum Dir
        {
            Local,
            In,
            Out,
            InOut,
        }
        private Dir _dir;
        private string _defaultValueExpr;

        public Dir VariableDir
        {
            get { return _dir; }
            set { _dir = value; }
        }

        public string DefaultValueExpr
        {
            get { return _defaultValueExpr; }
            set { _defaultValueExpr = value; }
        }

        public override void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            base.LoadFromXml(el, nsmgr);
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            VariableDir = (VariableDef.Dir)Enum.Parse(typeof(VariableDef.Dir), el.GetAttribute("dir"));
            DefaultValueExpr = SchemaUtil.GetXmlElementText(el, pr + "defaultValue", nsmgr);
        }
    }

}

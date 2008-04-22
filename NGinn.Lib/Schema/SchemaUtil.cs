using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Spring.Core.IO;
using Spring.Context;
using NLog;

namespace NGinn.Lib.Schema
{
    internal class SchemaUtil
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

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

        public static XmlReader GetPackageSchemaReader()
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            IResource rc = ctx.GetResource("assembly://NGinn.Lib/NGinn.Lib/PackageDefinition.xsd");
            return XmlReader.Create(rc.InputStream);
        }

        public static readonly string SCHEMA_NS = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// Load a variable definition from xml 
        /// </summary>
        /// <param name="el"></param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static VariableDef LoadVariable(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            VariableDef vd = new VariableDef();
            vd.Name = SchemaUtil.GetXmlElementText(el, pr + "name", nsmgr);
            vd.VariableType = SchemaUtil.GetXmlElementText(el, pr + "variableType", nsmgr);
            vd.IsArray = "true".Equals(SchemaUtil.GetXmlElementText(el, pr + "isArray", nsmgr));
            vd.VariableDir = (VariableDef.Dir)Enum.Parse(typeof(VariableDef.Dir), SchemaUtil.GetXmlElementText(el, pr + "dir", nsmgr));
            vd.VariableUsage = VariableDef.Usage.Optional;
            if ("true".Equals(SchemaUtil.GetXmlElementText(el, pr + "isRequired", nsmgr))) vd.VariableUsage = VariableDef.Usage.Required;
            vd.DefaultValueExpr = SchemaUtil.GetXmlElementText(el, pr + "defaultValue", nsmgr);
            return vd;
        }

        /// <summary>
        /// Read variable binding from xml
        /// </summary>
        /// <param name="el"></param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static VariableBinding LoadVariableBinding(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            VariableBinding vb = new VariableBinding();
            string vname = SchemaUtil.GetXmlElementText(el, pr + "variable", nsmgr);
            vb.VariableName = vname;
            string vtype = SchemaUtil.GetXmlElementText(el, pr + "bindingType", nsmgr);
            vb.BindingType = (VariableBinding.VarBindingType)Enum.Parse(typeof(VariableBinding.VarBindingType), vtype);
            if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
            {
                vb.BindingExpression = SchemaUtil.GetXmlElementText(el, pr + "sourceVariable", nsmgr);
            }
            else if (vb.BindingType == VariableBinding.VarBindingType.Xslt)
            {
                XmlElement e2 = (XmlElement)el.SelectSingleNode(pr + "bindingExpr", nsmgr);
                vb.BindingExpression = e2.InnerXml;
            }
            log.Debug("Loaded binding: {0}", vb.ToString());
            return vb;
        }

        /// <summary>
        /// Load 'DataDefinition'
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nsmgr"></param>
        /// <param name="variables"></param>
        /// <param name="inputBindings"></param>
        /// <param name="outputBindings"></param>
        public static void LoadDataSection(XmlElement data, XmlNamespaceManager nsmgr, IList<VariableDef> variables, IList<VariableBinding> inputBindings, IList<VariableBinding> outputBindings)
        {
            if (data == null) return;
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            foreach (XmlElement e2 in data.SelectNodes(string.Format("{0}variables/{0}variable", pr), nsmgr))
            {
                VariableDef vd = SchemaUtil.LoadVariable(e2, nsmgr);
                variables.Add(vd);
            }
            foreach (XmlElement e2 in data.SelectNodes(string.Format("{0}input-bindings/{0}binding", pr), nsmgr))
            {
                VariableBinding vb = SchemaUtil.LoadVariableBinding(e2, nsmgr);
                inputBindings.Add(vb);
            }
            foreach (XmlElement e2 in data.SelectNodes(string.Format("{0}output-bindings/{0}binding", pr), nsmgr))
            {
                VariableBinding vb = SchemaUtil.LoadVariableBinding(e2, nsmgr);
                outputBindings.Add(vb);
            }
        }

    }
}

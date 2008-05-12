using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Spring.Core.IO;
using Spring.Context;
using NLog;
using NGinn.Lib.Data;

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

        public static Flow LoadFlow(XmlElement el, XmlNamespaceManager nsmgr, ProcessDefinition pd)
        {
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            Flow fl = new Flow();
            string t = el.GetAttribute("from");
            fl.From = pd.GetNode(t);
            t = el.GetAttribute("to");
            fl.To = pd.GetNode(t);
            t = SchemaUtil.GetXmlElementText(el, pr + "inputCondition", nsmgr);
            fl.InputCondition = t;
            t = el.GetAttribute("evalOrder");
            if (t != null && t.Length > 0)
            {
                fl.EvalOrder = Int32.Parse(t);
            }
            t = el.GetAttribute("label");
            fl.Label = t;
            return fl;
        }

        /// <summary>
        /// Load a variable definition from xml 
        /// </summary>
        /// <param name="el"></param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static VariableDef LoadVariable(XmlElement el, XmlNamespaceManager nsmgr)
        {
            VariableDef vd = new VariableDef();
            vd.LoadFromXml(el, nsmgr);
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
            string vname = el.GetAttribute("variable");
            vb.VariableName = vname;
            string vtype = el.GetAttribute("bindingType");
            vb.BindingType = (VariableBinding.VarBindingType)Enum.Parse(typeof(VariableBinding.VarBindingType), vtype);
            if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
            {
                vb.BindingExpression = el.GetAttribute("sourceVariable");
                if (vb.BindingExpression == null || vb.BindingExpression.Length == 0) vb.BindingExpression = vb.VariableName;
            }
            else if (vb.BindingType == VariableBinding.VarBindingType.Xslt)
            {
                XmlElement e2 = (XmlElement)el.SelectSingleNode(pr + "bindingXslt", nsmgr);
                vb.BindingExpression = e2.InnerXml;
            }
            else if (vb.BindingType == VariableBinding.VarBindingType.Expr)
            {
                XmlElement e2 = (XmlElement)el.SelectSingleNode(pr + "expression", nsmgr);
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

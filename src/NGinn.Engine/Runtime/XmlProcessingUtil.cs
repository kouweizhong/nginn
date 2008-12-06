using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;
using NLog;
using NGinn.Lib.Schema;
using Mvp.Xml.Common;
using Mvp.Xml.Common.Xsl;
using NGinn.Lib.Data;

namespace NGinn.Engine.Runtime
{
    class XmlValidationMessage
    {
        public bool Error;
        public string Message;
    }

    class XmlProcessingUtil
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private class ValidationCtx
        {
            private List<XmlValidationMessage> _messages = new List<XmlValidationMessage>();
            private bool _hasError = false;

            public IList<XmlValidationMessage> Messages
            {
                get { return _messages; }
            }

            public bool HasError
            {
                get { return _hasError; }
            }

            public void ValidationEventHandler(object sender, ValidationEventArgs e)
            {
                XmlValidationMessage msg = new XmlValidationMessage();
                msg.Error = e.Severity == XmlSeverityType.Error ? true : false;
                msg.Message = e.Message;
                _messages.Add(msg);
                if (msg.Error) _hasError = true;
                log.Info("Validation {0}: {1}", e.Severity.ToString(), e.Message);
            }
        }

        public static bool ValidateXml(string xml, XmlSchemaSet schemas, IList<XmlValidationMessage> messages)
        {
            XmlReader xr = XmlReader.Create(new StringReader(xml));
            return ValidateXml(xr, schemas, messages);
        }

        public static bool ValidateXml(XmlReader xr, XmlSchemaSet schemas, IList<XmlValidationMessage> messages)
        {
            ValidationCtx ctx = new ValidationCtx();
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas = schemas;
            rs.ValidationEventHandler += new ValidationEventHandler(ctx.ValidationEventHandler);
            XmlReader xrv = XmlReader.Create(xr, rs);
            while (xrv.Read())
            {   
            }
            if (messages != null)
            {
                foreach (XmlValidationMessage msg in ctx.Messages)
                {
                    messages.Add(msg);
                }
            }
            return !ctx.HasError;
        }

        

        

        

        public void ReplaceNode(XmlNode insertNode, string xpath, XmlNode root)
        {
           
        }

        public void InsertSibling(XmlNode parentNode, XmlNode insertNode, string afterNodeName)
        {
            
        }

        /// <summary>
        /// Execute variable bindings on source doc and produce output doc.
        /// </summary>
        /// <param name="sourceDoc"></param>
        /// <param name="bindings"></param>
        public void ApplyVariableBindings(IXPathNavigable sourceDoc, IList<VariableBinding> bindings)
        {
            /*
            XslCompiledTransform tc = new XslCompiledTransform(true);
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.Indent = true;
            ws.OutputMethod = XmlOutputMethod.Xml;
            ws.OmitXmlDeclaration = true;
            tc.OutputSettings = ws;
            XsltArgumentList args = new XsltArgumentList();
            
            tc.Transform(sourceDoc, 
            */
        }

        public static readonly string XSL_NS = "http://www.w3.org/1999/XSL/Transform";

        private static void WriteBindingXsl(XmlWriter xw, VariableBinding binding)
        {
            if (binding.BindingType == VariableBinding.VarBindingType.Xslt)
            {
                xw.WriteRaw(binding.BindingExpression);
            }
            else if (binding.BindingType == VariableBinding.VarBindingType.CopyVar)
            {
                xw.WriteStartElement("for-each", XSL_NS);
                xw.WriteAttributeString("select", binding.BindingExpression);
                xw.WriteStartElement(binding.VariableName);
                xw.WriteStartElement("copy-of", XSL_NS);
                xw.WriteAttributeString("select", "node()");
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndElement();
            }
        }

        public static string PrepareXslTransform(IList<VariableBinding> bindings)
        {
            StringWriter sw = new StringWriter();
            XmlWriter xw = XmlWriter.Create(sw);
            xw.WriteStartElement("xsl", "stylesheet", XSL_NS);
            xw.WriteAttributeString("version", "1.0");
            xw.WriteStartElement("template", XSL_NS);
            xw.WriteAttributeString("match", "*");
            xw.WriteStartElement("bindingResult");
            foreach (VariableBinding vb in bindings)
            {
                WriteBindingXsl(xw, vb);
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.Flush();
            return sw.ToString();
        }

        public static XmlDocument CalculateVariableBindings(IXPathNavigable sourceXml, IList<VariableBinding> bindings)
        {
            string xslt = PrepareXslTransform(bindings);
            log.Info("XSLT: {0}", xslt);
            MvpXslTransform tr = new MvpXslTransform();
            StringReader sr = new StringReader(xslt);
            XmlReader xr = XmlReader.Create(sr);
            tr.Load(xr);
            XsltArgumentList al = new XsltArgumentList();
            StringWriter sw = new StringWriter();
            tr.Transform(new XmlInput(sourceXml), al, new XmlOutput(sw));
            log.Info("Transformed: {0}", sw.ToString());
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sw.ToString());
            return doc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceXml"></param>
        /// <param name="bindings"></param>
        /// <returns></returns>
        public static IDictionary<string, IList<XmlElement>> EvaluateVariableBindings(IXPathNavigable sourceXml, IList<VariableBinding> bindings)
        {
            XmlDocument doc = CalculateVariableBindings(sourceXml, bindings);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            Dictionary<string, IList<XmlElement>> dict = new Dictionary<string, IList<XmlElement>>();
            foreach (VariableBinding vb in bindings)
            {
                IList<XmlElement> lst;
                if (!dict.TryGetValue(vb.VariableName, out lst))
                {
                    lst = new List<XmlElement>();
                    dict.Add(vb.VariableName, lst);
                }
                XmlNodeList nodes = doc.DocumentElement.SelectNodes(vb.VariableName, nsmgr);
                if (nodes.Count == 0) 
                {
                    log.Warn("Binding did not return any data for variable " + vb.VariableName);
                }
                foreach (XmlElement el in nodes)
                {
                    lst.Add(el);
                }
            }
            return dict;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentNode">node containing variable values</param>
        /// <param name="variables">list of variable definitions</param>
        /// <param name="nsmgr">namespace manager</param>
        /// <returns>retrieved variable values</returns>
        public static IDictionary<string, IList<XmlElement>> RetrieveVariablesFromXml(XmlNode parentNode, IList<VariableDef> variables, XmlNamespaceManager nsmgr)
        {
            Dictionary<string, IList<XmlElement>> dict = new Dictionary<string, IList<XmlElement>>();
            foreach (VariableDef vd in variables)
            {
                IList<XmlElement> lst;
                if (!dict.TryGetValue(vd.Name, out lst))
                {
                    lst = new List<XmlElement>();
                    dict.Add(vd.Name, lst);
                }
                XmlNodeList nodelst = parentNode.SelectNodes(vd.Name, nsmgr);
                foreach(XmlElement el in nodelst)
                {
                    lst.Add(el);
                }
            }
            return dict;
        }

        /// <summary>
        /// Append variable values to XML
        /// </summary>
        /// <param name="parentNode">node where new values will be appended</param>
        /// <param name="values">values to append</param>
        /// <param name="variables">list of variable definitions</param>
        public static void InsertVariablesIntoXml(XmlNode parentNode, IDictionary<string, IList<XmlElement>> values, IList<VariableDef> variables)
        {
            foreach (VariableDef vd in variables)
            {
                IList<XmlElement> lst;
                if (values.TryGetValue(vd.Name, out lst))
                {
                    foreach(XmlNode n1 in lst)
                    {
                        XmlNode n2 = parentNode.OwnerDocument.ImportNode(n1, true);
                        parentNode.AppendChild(n2);
                    }
                }
            }
        }

        /// <summary>
        /// Create xml nodes for given variable specification
        /// </summary>
        /// <param name="parentNode">target parent node where new nodes will be added</param>
        /// <param name="variables">variable definition</param>
        /// <param name="sourceNode">source node containing input variables to be merged</param>
        /// <param name="nsmgr">namespace manager for selecting variables from sourceNode</param>
        public static void CreateInitialXml(XmlElement parentNode, IList<VariableDef> variables, XmlElement sourceNode, XmlNamespaceManager nsmgr)
        {
            foreach(VariableDef vd in variables)
            {
                XmlNodeList nl = sourceNode != null ? sourceNode.SelectNodes(vd.Name, nsmgr) : null;
                List<XmlNode> newNodes = new List<XmlNode>();
                if (nl != null && nl.Count > 0)
                {
                    foreach (XmlNode xn in nl)
                    {
                        newNodes.Add(parentNode.OwnerDocument.ImportNode(xn, true));
                    }
                }
                else
                {
                    XmlNode xn = parentNode.OwnerDocument.CreateElement(vd.Name);
                    if (vd.DefaultValueExpr != null) xn.InnerText = vd.DefaultValueExpr;
                    newNodes.Add(xn);
                }
                foreach(XmlNode xn in newNodes)
                {
                    parentNode.AppendChild(xn);
                }
            }

            //binding result: map: variable name -> List<XmlElement> (variable value)

        }

    }
}

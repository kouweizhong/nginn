using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Lib.Schema;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Mvp.Xml.Common.Xsl;
using System.Xml.Xsl;
using NGinn.Lib.Data;

namespace NGinnTest
{
    public class XmlTest
    {
        static Logger log = LogManager.GetCurrentClassLogger();
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
                        xw.WriteStartElement("value-of", XSL_NS);
                        xw.WriteAttributeString("select", ".");
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
                    /*xw.WriteStartElement("value-of", XSL_NS);
                    xw.WriteAttributeString("select", "name(.)");
                    xw.WriteEndElement();
                    */
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
        /// Pomys³: zrobiæ transfer danych read only
        /// czyli:
        /// bierzemy source Xml
        /// bierzemy target Xml
        /// bez modyfikacji source i target produkujemy xml nr 3 który jest wynikiem merge source i target.
        /// </summary>
        /// <param name="sourceXml"></param>
        /// <param name="targetVars"></param>
        /// <param name="bindings"></param>
        /// <param name="targetParent"></param>
        public static XmlElement TransferData(IXPathNavigable sourceXml, IList<VariableDef> targetVars, IList<VariableBinding> bindings, XmlElement targetParent)
        {
            XmlDocument bindingDoc = CalculateVariableBindings(sourceXml, bindings);
            //now merge doc to targetParent...
            //merging: replace contents of corresponding variables
            Dictionary<string, List<VariableBinding>> bindsDict = new Dictionary<string, List<VariableBinding>>();
            foreach (VariableBinding vb in bindings)
            {
                List<VariableBinding> lst;
                if (!bindsDict.TryGetValue(vb.VariableName, out lst))
                {
                    lst = new List<VariableBinding>(); bindsDict.Add(vb.VariableName, lst);
                }
                lst.Add(vb);
            }

            XmlElement outNode = (XmlElement)targetParent.OwnerDocument.ImportNode(targetParent, false);
            bool replace = true;
            for (int varN = 0; varN < targetVars.Count; varN++)
            {
                string vname = targetVars[varN].Name;
                List<XmlNode> newNodes = new List<XmlNode>();
                if (!bindsDict.ContainsKey(vname))
                {
                    XmlNodeList lst = targetParent.SelectNodes(vname);
                    foreach (XmlNode n in lst) newNodes.Add(n);
                }
                else
                {
                    XmlNodeList lst = bindingDoc.DocumentElement.SelectNodes(vname);
                    foreach (XmlNode n in lst) newNodes.Add(n);
                }
                foreach (XmlNode n in newNodes)
                {
                    XmlNode clone = outNode.OwnerDocument.ImportNode(n, true);
                    outNode.AppendChild(clone);
                }
            }
            log.Info("Merged doc: {0}", outNode.OuterXml);
            return outNode;
        }

        public static XmlElement Test1(string inputXmlFile, IList<VariableDef> targetVars, IList<VariableBinding> bindings, XmlElement targetNode)
        {
            XmlDocument doc = new XmlDocument();
            log.Info("Loading {0}", inputXmlFile);
            doc.Load(inputXmlFile);
            XmlElement newTarget = TransferData(doc.DocumentElement, targetVars, bindings, targetNode);
            return newTarget;
        }
    }
}

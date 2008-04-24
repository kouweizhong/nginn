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

namespace NGinn.Engine.Runtime
{
    class XmlProcessingUtil
    {
        public void ValidateXml(string xml)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            XmlSchemaCollection coll = new XmlSchemaCollection();
            
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

    }
}

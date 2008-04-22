using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class Package
    {
        public static readonly string PACKAGE_NAMESPACE = "http://www.nginn.org/PackageDefinition.1_0";
        private string _name;
        private List<string> _schemaFiles = new List<string>();
        private List<string> _processFiles = new List<string>();

        public string PackageName
        {
            get { return _name; }
            set { _name = value; }
        }

        public IList<string> SchemaFiles
        {
            get { return _schemaFiles; }
        }

        public IList<string> ProcessFiles
        {
            get { return _processFiles; }
        }

        public void LoadXml(Stream xmlStm)
        {
            XmlDocument doc = new XmlDocument();
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;

            XmlReader schemaRdr = SchemaUtil.GetPackageSchemaReader();
            rs.Schemas.Add(PACKAGE_NAMESPACE, schemaRdr);
            using (XmlReader xr = XmlReader.Create(xmlStm, rs))
            {
                doc.Load(xr);
            }
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace(string.Empty, PACKAGE_NAMESPACE);
            nsmgr.AddNamespace("wf", PACKAGE_NAMESPACE);
            _name = doc.DocumentElement.GetAttribute("name");
            _schemaFiles = new List<string>();
            foreach (XmlElement el in doc.SelectNodes("wf:package/wf:dataSchemas/wf:schema", nsmgr))
            {
                _schemaFiles.Add(el.InnerText);
            }
            _processFiles = new List<string>();
            foreach (XmlElement el in doc.SelectNodes("wf:package/wf:processDefinitions/wf:process", nsmgr))
            {
                _processFiles.Add(el.InnerText);
            }
        }
    }
}

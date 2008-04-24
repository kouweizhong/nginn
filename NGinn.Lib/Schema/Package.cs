using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Workflow package definition.
    /// Package contains processes, additional data schemas and all other objects necessary
    /// </summary>
    [Serializable]
    public class PackageDefinition
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

        /// <summary>
        /// Return list of base process names (without version information)
        /// </summary>
        /// <returns></returns>
        public string GetProcessNames()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return list of process versions available for given process base name
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public IList<int> GetProcessVersions(string processName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return process definition for given name.version
        /// If version is not specified, default version is returned
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ProcessDefinition GetProcessDefinition(string name)
        {
            throw new NotImplementedException();
        }
    }
}

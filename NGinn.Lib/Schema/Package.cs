using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using NGinn.Lib.Services;
using NLog;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Workflow package definition.
    /// Package contains processes, additional data schemas and all other objects necessary
    /// TODO: fix the serialization of PackageDefinition and ProcessDefinition
    /// TODO: currently they try to serialize too much
    /// </summary>
    [Serializable]
    public class PackageDefinition
    {
        public static readonly string PACKAGE_NAMESPACE = "http://www.nginn.org/PackageDefinition.1_0";
        private string _name;
        private List<string> _schemaFiles = new List<string>();
        private List<string> _processFiles = new List<string>();
        private IProcessPackageStore _ds;
        private TypeSet _packageTypes = new TypeSet();
        

        /// <summary>
        /// Map: process name->list(processDefInformation) - for keeping
        /// information about processes in the package
        /// </summary>
        private Dictionary<string, List<ProcessDefInformation>> _processInfoCache = null;
        

        private static Logger log = LogManager.GetCurrentClassLogger();

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

        protected IProcessPackageStore DataStore
        {
            get { return _ds; }
            set { _ds = value; }
        }

        /// <summary>
        /// Package-level data type definitions
        /// </summary>
        public TypeSet PackageTypes
        {
            get { return _packageTypes; }
        }

        public void Load(IProcessPackageStore store)
        {
            DataStore = store;
            using (Stream stm = store.GetPackageDefinitionStream())
            {
                LoadXml(stm);
            }
        }

        protected void LoadXml(Stream xmlStm)
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
            LoadProcessInformationIfNecessary();
        }

        /// <summary>
        /// Return list of base process names (without version information)
        /// </summary>
        /// <returns></returns>
        public IList<string> GetProcessNames()
        {
            List<string> lst = new List<string>(ProcessInfoCache.Keys);
            lst.Sort();
            return lst;
        }

        /// <summary>
        /// Return list of process versions available for given process base name
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public IList<int> GetProcessVersions(string processName)
        {
            List<ProcessDefInformation> lst;
            if (!ProcessInfoCache.TryGetValue(processName, out lst)) throw new ApplicationException("Invalid process name");
            List<int> versions = new List<int>();
            foreach (ProcessDefInformation pdi in lst)
            {
                versions.Add(pdi.Version);
            }
            versions.Sort();
            return versions;
        }

        protected ProcessDefInformation GetProcessInfo(string procName, int version)
        {
            List<ProcessDefInformation> lst;
            if (!ProcessInfoCache.TryGetValue(procName, out lst)) throw new ApplicationException("Invalid process name");
            foreach (ProcessDefInformation pdi in lst)
            {
                if (pdi.Version == version)
                    return pdi;
            }
            return null;
        }

        protected ProcessDefInformation GetProcessInfo(string procFullName)
        {
            int idx = procFullName.IndexOf('.');
            int ver = -1;
            string name = procFullName;
            if (idx > 0)
            {
                name = procFullName.Substring(0, idx);
                ver = Int32.Parse(procFullName.Substring(idx + 1));
            }
            return GetProcessInfo(name, ver);
        }

        public string GetProcessFileName(string procName, int version)
        {
            ProcessDefInformation pdi = GetProcessInfo(procName, version);
            return pdi == null ? null : pdi.FileName;
        }

        public string GetProcessFileName(string procFullName)
        {
            ProcessDefInformation pdi = GetProcessInfo(procFullName);
            return pdi == null ? null : pdi.FileName;
        }

        /// <summary>
        /// Return process definition for given name.version
        /// If version is not specified, default version is returned
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ProcessDefinition GetProcessDefinition(string name)
        {
            return DataStore.GetProcessDefinition(name);
        }

        

        public string GetSchema(string schemaFile)
        {
            if (!_schemaFiles.Contains(schemaFile)) throw new ArgumentException("Invalid schema file name", "schemaFile");
            using (Stream stm = DataStore.GetPackageContentStream(schemaFile))
            {
                StreamReader sr = new StreamReader(stm, Encoding.UTF8);
                return sr.ReadToEnd();
            }
        }

        protected void LoadPackageDataTypes()
        {
            _packageTypes = new TypeSet();
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas.XmlResolver = new AssemblyResourceXmlResolver();
            rs.Schemas.Add(ProcessDefinition.WORKFLOW_NAMESPACE, "TypeSetDefinition.xsd");

            foreach (string fileName in _schemaFiles)
            {
                try
                {
                    log.Info("Will load type definitions from file: {0}", fileName);
                    using (Stream stm = DataStore.GetPackageContentStream(fileName))
                    {
                        XmlDocument doc = new XmlDocument();
                        using (XmlReader xr = XmlReader.Create(stm, rs))
                        {
                            doc.Load(xr);
                        }
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                        nsmgr.AddNamespace(string.Empty, ProcessDefinition.WORKFLOW_NAMESPACE);
                        nsmgr.AddNamespace("wf", ProcessDefinition.WORKFLOW_NAMESPACE);
                        _packageTypes.LoadXml(doc.DocumentElement, nsmgr);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error loading package schema file: {0}.{1}", PackageName, fileName);
                    throw;
                }
            }
        }

        

        protected Dictionary<String, List<ProcessDefInformation>> ProcessInfoCache
        {
            get
            {
                Dictionary<String, List<ProcessDefInformation>> pc = LoadProcessInformationIfNecessary();
                return pc;
            }
        }

        /// <summary>
        /// Scan package files for process information
        /// </summary>
        /// <returns></returns>
        protected Dictionary<String, List<ProcessDefInformation>> LoadProcessInformationIfNecessary()
        {
            lock (this)
            {
                if (_processInfoCache != null) return _processInfoCache;
                Dictionary<string, List<ProcessDefInformation>> cache = new Dictionary<string, List<ProcessDefInformation>>();
                foreach (string file in _processFiles)
                {
                    log.Info("Loading process file : {0}", file);
                    ProcessDefInformation pdi = new ProcessDefInformation();
                    pdi.FileName = file;
                    using (Stream stm = DataStore.GetPackageContentStream(file))
                    {
                        XmlReader xr = XmlReader.Create(stm);
                        XmlNodeType ntype = xr.MoveToContent();
                        if (ntype != XmlNodeType.Element) throw new Exception("Expected root element node");
                        string pname = xr.GetAttribute("name");
                        if (pname == null) throw new Exception("Missing 'name' attribute");
                        string pver = xr.GetAttribute("version");
                        if (pver == null) throw new Exception("Missing 'version' attribute");
                        pdi.Name = pname;
                        pdi.Version = Int32.Parse(pver);
                    }
                    List<ProcessDefInformation> lst;
                    if (!cache.TryGetValue(pdi.Name, out lst))
                    {
                        lst = new List<ProcessDefInformation>(); cache.Add(pdi.Name, lst);
                    }
                    foreach (ProcessDefInformation pdi2 in lst)
                    {
                        if (pdi2.Version == pdi.Version &&
                            pdi2.Name == pdi.Name)
                            throw new ApplicationException(string.Format("Process {0}.{1} already defined in file {2}", pdi2.Name, pdi2.Version, pdi2.FileName));
                    }
                    lst.Add(pdi);
                }

                foreach (List<ProcessDefInformation> lst in cache.Values)
                {
                    lst.Sort(new Comparison<ProcessDefInformation>(ProcessDefInformation.Compare));
                }
                _processInfoCache = cache;
                return _processInfoCache;
            }
        }
        
        [Serializable]
        protected class ProcessDefInformation
        {
            public string Name;
            public int Version;
            public string FileName;

            public static int Compare(ProcessDefInformation left, ProcessDefInformation right)
            {
                int ret = String.Compare(left.Name, right.Name);
                if (ret != 0) return ret;
                if (left.Version < right.Version)
                {
                    return -1;
                }
                else return left.Version == right.Version ? 0 : 1;
            }
        }
    }

    
}

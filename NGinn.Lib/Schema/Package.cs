using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using NGinn.Lib.Services;
using NLog;
using NGinn.Lib.Data;

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
        private IPackageDataStore _ds;
        private TypeSet _packageTypes = new TypeSet();
        

        /// <summary>
        /// Map: process name->list(processDefInformation) - for keeping
        /// information about processes in the package
        /// </summary>
        private Dictionary<string, List<ProcessDefInformation>> _processInfoCache = null;
        
        /// <summary>
        /// process definition cache. Not serialized to save bandwidth.
        /// </summary>
        [NonSerialized]
        private Dictionary<string, ProcessDefinition> _definitionCache = null;

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

        protected IPackageDataStore DataStore
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

        /// <summary>
        /// Return process definition for given name.version
        /// If version is not specified, default version is returned
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ProcessDefinition GetProcessDefinition(string name)
        {
            int ver = -1;
            string nameBase = name;
            int idx = name.IndexOf('.');
            if (idx >= 0)
            {
                ver = Int32.Parse(name.Substring(idx + 1));
                nameBase = name.Substring(0, idx);
            }
            else
            {
                ver = -1;
            }
            List<ProcessDefInformation> lpdi;
            if (!ProcessInfoCache.TryGetValue(nameBase, out lpdi))
            {
                throw new ApplicationException("Process not found: " + nameBase);
            }
            ProcessDefInformation pdi = null;
            if (ver < 0)
                pdi = lpdi[lpdi.Count - 1];
            else
            {
                foreach (ProcessDefInformation p in lpdi)
                {
                    if (p.Version == ver)
                    {
                        pdi = p;
                        break;
                    }
                }
            }
            if (pdi == null) throw new ApplicationException(string.Format("Process version not found: {0}", name));
            return GetProcessDefinition(pdi);
        }

        /// <summary>
        /// Get specified process definition
        /// Load from file if necessary.
        /// </summary>
        /// <param name="pdi"></param>
        /// <returns></returns>
        protected ProcessDefinition GetProcessDefinition(ProcessDefInformation pdi)
        {
            lock (this)
            {
                if (_definitionCache == null)
                    _definitionCache = new Dictionary<string, ProcessDefinition>();
                ProcessDefinition pd;
                string name = string.Format("{0}.{1}.{2}", PackageName, pdi.Name, pdi.Version);
                if (_definitionCache.TryGetValue(name, out pd)) return pd;
                log.Info("Will load process definition {0}.{1} from file {2}", pdi.Name, pdi.Version, pdi.FileName);
                pd = new ProcessDefinition();
                using (Stream stm = DataStore.GetPackageContentStream(pdi.FileName))
                {
                    pd.Package = this;
                    pd.Load(stm);
                }
                _definitionCache[name] = pd;
                return pd;
            }
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
            foreach (string fileName in _schemaFiles)
            {
                log.Info("Will load type definitions from file: {0}", fileName);
                using (Stream stm = DataStore.GetPackageContentStream(fileName))
                {
                    
                }
            }
        }

        /// <summary>
        /// Load package definition from given data store
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static PackageDefinition Load(IPackageDataStore ds)
        {
            PackageDefinition pd = new PackageDefinition();
            pd._ds = ds;
            using (Stream stm = ds.GetPackageDefinitionStream())
            {
                pd.LoadXml(stm);
            }
            return pd;
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

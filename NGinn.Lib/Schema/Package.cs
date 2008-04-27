using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using NGinn.Lib.Services;
using NLog;

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
        private IPackageDataStore _ds;
        private Dictionary<string, List<ProcessDefInformation>> _processCache = null;
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
        }

        /// <summary>
        /// Return list of base process names (without version information)
        /// </summary>
        /// <returns></returns>
        public IList<string> GetProcessNames()
        {
            List<string> lst = new List<string>(ProcessCache.Keys);
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
            if (!ProcessCache.TryGetValue(processName, out lst)) throw new ApplicationException("Invalid process name");
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
            if (!ProcessCache.TryGetValue(nameBase, out lpdi))
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
            return pdi.Process;
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

        protected Dictionary<String, List<ProcessDefInformation>> ProcessCache
        {
            get
            {
                Dictionary<String, List<ProcessDefInformation>> pc = LoadProcessInformationIfNecessary();
                return pc;
            }
        }


        protected Dictionary<String, List<ProcessDefInformation>> LoadProcessInformationIfNecessary()
        {
            lock (this)
            {
                if (_processCache != null) return _processCache;
                Dictionary<string, List<ProcessDefInformation>> cache = new Dictionary<string, List<ProcessDefInformation>>();
                foreach (string file in _processFiles)
                {
                    log.Info("Loading process file : {0}", file);
                    ProcessDefInformation pdi = new ProcessDefInformation();
                    pdi.FileName = file;
                    using (Stream stm = DataStore.GetPackageContentStream(file))
                    {
                        pdi.Process = new ProcessDefinition();
                        pdi.Process.Package = this;
                        pdi.Process.Load(stm);
                    }
                    pdi.Name = pdi.Process.Name;
                    pdi.Version = pdi.Process.Version;
                    List<ProcessDefInformation> lst;
                    if (!cache.TryGetValue(pdi.Name, out lst))
                    {
                        lst = new List<ProcessDefInformation>(); cache.Add(pdi.Name, lst);
                    }
                    lst.Add(pdi);
                }

                foreach (List<ProcessDefInformation> lst in cache.Values)
                {
                    lst.Sort(new Comparison<ProcessDefInformation>(ProcessDefInformation.Compare));
                }
                _processCache = cache;
                return _processCache;
            }
        }
        
        protected class ProcessDefInformation
        {
            public string Name;
            public int Version;
            public string FileName;
            public ProcessDefinition Process;


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

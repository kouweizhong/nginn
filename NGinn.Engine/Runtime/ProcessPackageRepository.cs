using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NLog;
using System.IO;
using NGinn.Lib.Schema;
using System.Xml;

namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// Package repository storing packages in a directory
    /// </summary>
    public class FSProcessPackageRepository : IProcessPackageRepository
    {
        private string _baseDir;
        
        private Dictionary<string, FSProcessPackageStore> _packageCache;

        private Logger log = LogManager.GetCurrentClassLogger();

        #region IProcessPackageRepository Members
        public IList<string> PackageNames
        {
            get
            {
                lock (this)
                {
                    if (_packageCache == null) ScanForPackages();
                    List<string> names = new List<string>(_packageCache.Keys);
                    names.Sort();
                    return names;
                }
            }
        }
        #endregion

        private void ScanForPackages()
        {
            log.Info("Scanning directory {0} for packages", _baseDir);
            Dictionary<string, FSProcessPackageStore> names = new Dictionary<string, FSProcessPackageStore>();
            
            if (Directory.Exists(_baseDir))
            {
                string[] pkgs = Directory.GetFiles(_baseDir, "*.ngpk", SearchOption.AllDirectories);
                foreach (string pkg in pkgs)
                {
                    log.Info("Found package file {0}", pkg);
                    try
                    {
                        FSProcessPackageStore store = new FSProcessPackageStore(pkg);
                        names[store.PackageDef.PackageName] = store;
                        log.Info("Successfully loaded package {0} from {1}", store.PackageDef.PackageName, pkg);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error reading package file: {0}: {1}", pkg, ex);
                    }
                }
            }
            _packageCache = names;
        }

        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }




        #region IProcessPackageRepository Members


        public IProcessPackageStore GetPackage(string name)
        {
            lock (this)
            {
                if (_packageCache == null) ScanForPackages();
                if (!_packageCache.ContainsKey(name)) throw new ApplicationException("Unknown package: " + name);
                return _packageCache[name];
            }
        }

        public void AddPackage(Stream packageInputStream)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void UpdatePackage(Stream packageInputStream)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }

    class FSProcessPackageStore : IProcessPackageStore
    {
        private Package _pkg;
        private Dictionary<string, ProcessDefinition> _processes = null;
        private string _packageFile;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public FSProcessPackageStore(string packageFileName)
        {
            _pkg = new Package();
            using (FileStream fs = new FileStream(packageFileName, FileMode.Open))
            {
                _pkg.LoadXml(fs);
            }
            _packageFile = packageFileName;
        }

        protected void InitializeProcessInformation()
        {
            Dictionary<string, ProcessDefinition> procs = new Dictionary<string, ProcessDefinition>();
            string baseDir = Path.GetDirectoryName(_packageFile);
            log.Debug("Loading package {0} files from directory: {1}", _pkg.PackageName, baseDir);
            foreach (string fn in _pkg.ProcessFiles)
            {
                string tfn = fn;
                if (!Path.IsPathRooted(fn)) tfn = Path.Combine(baseDir, fn);
                log.Debug("Trying to load process definition: {0}", tfn);
                ProcessDefinition pd = new ProcessDefinition();
                pd.LoadXmlFile(tfn);
                string nv = string.Format("{0}.{1}", pd.Name, pd.Version);
                procs[nv] = pd;
            }
            _processes = procs;
        }

        #region IProcessPackageStore Members

        public Package PackageDef
        {
            get { return _pkg; }
        }

        public string PackageFile
        {
            get { return _packageFile; }
        }

        public Stream OpenPackageStream(string fileName)
        {
            string baseDir = Path.GetDirectoryName(_packageFile);
            return new FileStream(Path.Combine(baseDir, fileName), FileMode.Open);
        }

        public void AddProcessDefinition(string definitionXml)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void AddSchema(string schemaXml)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IProcessPackageStore Members


        public ProcessDefinition GetProcessDefinition(string name)
        {
            Dictionary<string, ProcessDefinition> proc = _processes;
            lock (this)
            {
                if (proc == null) InitializeProcessInformation();
                proc = _processes;
            }
            string nv = name;
            if (!proc.ContainsKey(nv)) throw new ApplicationException("Process not found: " + nv);
            return proc[nv];
        }

        #endregion
    }

}

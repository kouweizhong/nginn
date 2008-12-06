using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Lib.Schema;
using System.IO;

namespace NGinn.Lib.Services
{

    /// <summary>
    /// Handling reload
    /// If package file is modified (ngpkg), reload whole package.
    /// Until package is successfully loaded don't replace it in the cache.
    /// If single process file is modified, reload only that process.
    /// Until process is successfully loaded, don't replace it in the cache.
    /// </summary>
    public class FSProcessPackageLoader 
    {
        private Logger log = LogManager.GetCurrentClassLogger();

        private string _baseDIr;
        public string BaseDirectory
        {
            get { return _baseDIr; }
            set { _baseDIr = value; }
        }

        protected class CachedPackage
        {
            public PackageDefinition PackageDef;
            public string PackageFile;
            public DateTime DateLoaded;
            public List<CachedProcess> CachedProcesses = new List<CachedProcess>();

            public void CheckProcessFileModifications()
            {

            }
        }

        protected class CachedProcess
        {
            public ProcessDefinition ProcessDef;
            public string ProcessFile;
            public DateTime DateLoaded;
        }

        private Dictionary<string, CachedPackage> _pkgCache = new Dictionary<string, CachedPackage>();


        public PackageDefinition GetPackageDefinition(string packageName)
        {
            lock (this)
            {
                CachedPackage pk;
                if (_pkgCache.TryGetValue(packageName, out pk))
                {
                    return pk.PackageDef;
                }
                FileSystemWatcher fsw = new FileSystemWatcher(BaseDirectory);
                fsw.IncludeSubdirectories = true;
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                
            }
            return null;
        }

        public ProcessDefinition GetProcessDefinition(string definitionId)
        {
            return null;
        }

        protected CachedPackage GetPackageForFileName(string fileName)
        {
            foreach (CachedPackage cp in _pkgCache.Values)
            {
                if (string.Compare(cp.PackageFile, fileName, true) == 0) return cp;
            }
            return null;
        }
        /// <summary>
        /// Rescan the base directory and detect changes.
        /// If new package is found, load it.
        /// If package file has been deleted, don't unload it.
        /// If package file has been modified, reload the package.
        /// If process definition has been modified, reload only the process.
        /// </summary>
        protected void RescanBaseDirectory()
        {
            if (!Directory.Exists(BaseDirectory))
            {
                log.Info("Creating base directory: {0}", BaseDirectory);
                Directory.CreateDirectory(BaseDirectory);
            }
            log.Debug("Scanning directory {0} for packages", BaseDirectory);
            string[] pkgFiles = Directory.GetFiles(BaseDirectory, "*.ngpkg", SearchOption.AllDirectories);
            foreach (string pkgFile in pkgFiles)
            {
                log.Debug("Checking file {0}", pkgFile);
                CachedPackage cp = GetPackageForFileName(pkgFile);
                bool reloadPackage = false;
                if (cp == null)
                {
                    reloadPackage = true;
                }
                else
                {
                    DateTime dt = File.GetLastWriteTime(pkgFile);
                    if (dt > cp.DateLoaded)
                    {
                        reloadPackage = true;
                    }
                }
                if (reloadPackage)
                {
                    
                    CachedPackage cp2 = LoadPackage(pkgFile);
                }
                else
                {
                    cp.CheckProcessFileModifications();
                }
            }
        }

        protected CachedPackage LoadPackage(string packageFile)
        {
            log.Info("Loading package file {0}", packageFile);
            return null;
        }

        
    }
}

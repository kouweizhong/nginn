using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NLog;
using System.IO;
using NGinn.Lib.Schema;
using NGinn.Lib.Services;
using System.Xml;
using NGinn.Lib.Interfaces;

namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// Package repository storing packages in a directory
    /// Handles script compilation on process definition reload
    /// </summary>
    public class FSProcessPackageRepository : IProcessPackageRepository
    {
        private string _baseDir;

        private class PackageInfo
        {
            public ProcessPackageStore PackageStore;
        }

        private Dictionary<string, PackageInfo> _packageCache;
        private Logger log = LogManager.GetCurrentClassLogger();

        private IProcessScriptManager _scriptManager;

        /// <summary>
        /// Script manager for the repository
        /// </summary>
        public IProcessScriptManager ScriptManager
        {
            get { return _scriptManager; }
            set { _scriptManager = value; }
        }

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
            lock (this)
            {
                log.Info("Scanning directory {0} for packages", _baseDir);
                Dictionary<string, PackageInfo> names = new Dictionary<string, PackageInfo>();

                if (Directory.Exists(_baseDir))
                {
                    string[] pkgs = Directory.GetFiles(_baseDir, "*.ngpk", SearchOption.AllDirectories);
                    foreach (string pkg in pkgs)
                    {
                        log.Info("Found package file {0}", pkg);
                        try
                        {
                            ProcessPackageStore ps = new ProcessPackageStore(pkg);
                            PackageDefinition pd = ps.GetPackageDefinition();
                            log.Info("Successfully loaded package {0} from {1}", pd.PackageName, pkg);
                            PackageInfo pi = new PackageInfo();
                            pi.PackageStore = ps;
                            pi.PackageStore.ProcessReload += new ProcessPackageStore.ProcessReloadingDelegate(PackageStore_ProcessReload);
                            names[pd.PackageName] = pi;
                        }
                        catch (Exception ex)
                        {
                            log.Error("Error reading package file: {0}: {1}", pkg, ex);
                        }
                    }
                }
                _packageCache = names;
            }
        }

        void PackageStore_ProcessReload(ProcessDefinition pd)
        {
            if (ScriptManager != null)
            {
                ScriptManager.ProcessDefinitionUpdated(pd);
            }
        }

        /// <summary>
        /// Process repository location
        /// </summary>
        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }




        #region IProcessPackageRepository Members


        public PackageDefinition GetPackage(string name)
        {
            lock (this)
            {
                if (_packageCache == null) ScanForPackages();
                if (!_packageCache.ContainsKey(name)) throw new ApplicationException("Unknown package: " + name);
                PackageInfo pi = _packageCache[name];
                return pi.PackageStore.GetPackageDefinition();
            }
        }

        public ProcessDefinition GetProcess(string name)
        {
            int idx = name.IndexOf('.');
            string pkgName = name.Substring(0, idx);
            PackageDefinition pkg = GetPackage(pkgName);
            if (pkg == null) throw new Exception("Package not found: " + pkgName);
            return pkg.GetProcessDefinition(name.Substring(idx + 1));
        }

        #endregion
    }
}

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
    /// </summary>
    public class FSProcessPackageRepository : IProcessPackageRepository
    {
        private string _baseDir;
        
        private Dictionary<string, PackageDefinition> _packageCache;

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
            Dictionary<string, PackageDefinition> names = new Dictionary<string, PackageDefinition>();
            
            if (Directory.Exists(_baseDir))
            {
                string[] pkgs = Directory.GetFiles(_baseDir, "*.ngpk", SearchOption.AllDirectories);
                foreach (string pkg in pkgs)
                {
                    log.Info("Found package file {0}", pkg);
                    try
                    {
                        FSPackageDataStore ds = new FSPackageDataStore(pkg);
                        PackageDefinition pd = PackageDefinition.Load(ds);
                        log.Info("Successfully loaded package {0} from {1}", pd.PackageName, pkg);
                        names[pd.PackageName] = pd;
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


        public PackageDefinition GetPackage(string name)
        {
            lock (this)
            {
                if (_packageCache == null) ScanForPackages();
                if (!_packageCache.ContainsKey(name)) throw new ApplicationException("Unknown package: " + name);
                return _packageCache[name];
            }
        }

        #endregion
    }
}

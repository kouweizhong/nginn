using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;
using NLog;
using System.IO;
using System.Diagnostics;

namespace NGinn.Lib.Services
{
    /// <summary>
    /// Process package store.
    /// Handles loading package files and process definitions.
    /// </summary>
    public class ProcessPackageStore : IProcessPackageStore
    {
        private Logger log = LogManager.GetCurrentClassLogger();
        private string _pkgFile;
        private PackageDefinition _package;
        private class ProcessInfo
        {
            public ProcessDefinition ProcessDef;
            public string ProcessFileName;
            public DateTime LoadedDate;
        }

        private Dictionary<string, ProcessInfo> _processes = new Dictionary<string, ProcessInfo>();


        public ProcessPackageStore()
        {
        }

        public ProcessPackageStore(string packageFile)
        {
            _pkgFile = packageFile;
        }

        public string PackageFile
        {
            get { return _pkgFile; }
            set { _pkgFile = value; }
        }

        public string BaseDirectory
        {
            get { return Path.GetDirectoryName(_pkgFile); }
        }

        public void ReloadPackage()
        {
            PackageDefinition pd = new PackageDefinition();
            pd.Load(this);
            _package = pd;
        }

        public void ReloadProcess(string processName)
        {

        }


        #region IProcessPackageStore Members

        public PackageDefinition GetPackageDefinition()
        {
            if (_package != null) return _package;
            lock (this)
            {
                if (_package != null) return _package;
                ReloadPackage();
                return _package;
            }
            return _package;
        }

        private ProcessInfo GetProcessInfo(string name)
        {
            ProcessInfo pi;
            return _processes.TryGetValue(name, out pi) ? pi : null;
        }

        public ProcessDefinition GetProcessDefinition(string name)
        {
            ProcessInfo pi = GetProcessInfo(name);
            if (pi != null) return pi.ProcessDef;
            PackageDefinition pd = GetPackageDefinition();
            Debug.Assert(pd != null);
            lock (this)
            {
                pi = GetProcessInfo(name);
                if (pi != null) return pi.ProcessDef;

                pi = new ProcessInfo();
                pi.ProcessFileName = pd.GetProcessFileName(name);
                log.Info("Will load process definition {0} from file {1}", name , pi.ProcessFileName);
                ProcessDefinition pdi = new ProcessDefinition();
                pdi.Package = pd;
                using (Stream stm = GetPackageContentStream(pi.ProcessFileName))
                {
                    pdi.Load(stm);
                }
                pi.ProcessDef = pdi;
                pi.LoadedDate = DateTime.Now;
                if (this.ProcessReload != null) this.ProcessReload(pdi);
                _processes[name] = pi;
                return pi.ProcessDef;
            }
        }

        public ProcessDefinition GetProcessDefinition(string name, int version)
        {
            throw new NotImplementedException();
        }

        public Stream GetPackageContentStream(string contentName)
        {
            string fp = Path.Combine(BaseDirectory, contentName);
            return File.Open(fp, FileMode.Open, FileAccess.Read);
        }

        public Stream GetPackageDefinitionStream()
        {
            Stream stm = File.Open(PackageFile, FileMode.Open, FileAccess.Read);
            return stm;
        }

        #endregion

        public void DetectModificationsAndReload()
        {

        }

        public delegate void ProcessReloadingDelegate(ProcessDefinition pd);
        public event ProcessReloadingDelegate ProcessReload;

    }
}

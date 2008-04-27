using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Runtime
{
    public class ProcessDefinitionRepository : IProcessDefinitionRepository
    {
        private IProcessPackageRepository _packageRepository;

        public IProcessPackageRepository PackageRepository
        {
            get { return _packageRepository; }
            set { _packageRepository = value; }
        }

        #region IProcessDefinitionRepository Members

        public NGinn.Lib.Schema.ProcessDefinition GetProcessDefinition(string definitionId)
        {
            int idx = definitionId.IndexOf('.');
            if (idx < 0) throw new Exception("Expected process id as PackageName.ProcessName.Version");
            string pkgName = definitionId.Substring(0, idx);
            string procName = definitionId.Substring(idx + 1);
            PackageDefinition pd = PackageRepository.GetPackage(pkgName);
            if (pd == null) throw new ApplicationException("Unknown package: " + pkgName);
            ProcessDefinition proc = pd.GetProcessDefinition(procName);
            return proc;
        }

        public string GetProcessDefinitionId(string packageName, string processName, int version)
        {
            return string.Format("{0}.{1}.{2}", packageName, processName, version);
        }

        #endregion

        #region IProcessDefinitionRepository Members


        public string GetProcessInputSchema(string definitionId)
        {
            ProcessDefinition pd = GetProcessDefinition(definitionId);
            return pd.GenerateInputSchema();
        }

        #endregion
    }
}

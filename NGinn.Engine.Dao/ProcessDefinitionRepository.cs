using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using Sooda;
using NLog;
using NGinn.Lib.Schema;
using System.IO;
using NGinn.Engine.Dao.TypedQueries;
using System.Collections;

namespace NGinn.Engine.Dao
{
    
    public class ProcessDefinitionRepository : IProcessDefinitionRepository
    {
        private Dictionary<string, ProcessDefinition> _pdCache = new Dictionary<string, ProcessDefinition>();
       


        

        

        private ProcessDefinitionDb FindProcessDefinitionInternal(string name, int version)
        {
            ProcessDefinitionDbList lst = ProcessDefinitionDb.GetList(ProcessDefinitionDbField.Name == name && ProcessDefinitionDbField.Version == version);
            if (lst.Count == 0) return null;
            if (lst.Count > 1) throw new Exception("Not unique name/version");
            return lst[0];
        }

        public void DeleteProcessDefinition(string definitionId)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ProcessDefinition GetProcessDefinition(string definitionId)
        {
            ProcessDefinition pd;
            lock (this)
            {
                if (_pdCache.TryGetValue(definitionId, out pd))
                    return pd;
                using (SoodaTransaction st = new SoodaTransaction())
                {
                    ProcessDefinitionDb pdb = ProcessDefinitionDb.Load(Int32.Parse(definitionId));
                    pd = new ProcessDefinition();
                    pd.LoadXml(pdb.ProcessXml);
                    _pdCache[pdb.Id.ToString()] = pd;
                    return pd;
                }
            }
        }

        public IList<string> GetProcessDefinitionNames()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IList<int> GetProcessDefinitionVersions(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IList<string> GetProcessDefinitionIds()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #region IProcessDefinitionRepository Members

        public string InsertProcessDefinition(string pdXml)
        {
            ProcessDefinition pd = new ProcessDefinition();
            pd.LoadXml(pdXml);
            List<ValidationMessage> msgs = new List<ValidationMessage>();
            pd.Validate(msgs);
            foreach (ValidationMessage vm in msgs)
            {
                if (vm.IsError) throw new Exception("Process definition invalid: " + vm.Message);
            }
            using (SoodaTransaction st = new SoodaTransaction())
            {
                ProcessDefinitionDb t = FindProcessDefinitionInternal(pd.Name, pd.Version);
                if (t != null) throw new Exception(string.Format("Process definition {0}.{1} already exists", pd.Name, pd.Version)); 
                ProcessDefinitionDb pdb = new ProcessDefinitionDb();
                pdb.Name = pd.Name;
                pdb.ProcessXml = pdXml;
                pdb.Version = pd.Version;
                st.Commit();
                return pdb.Id.ToString();
            }
        }

        public void UpdateProcessDefinition(string definitionXml)
        {
            ProcessDefinition pd = new ProcessDefinition();
            pd.LoadXml(definitionXml);
            List<ValidationMessage> msgs = new List<ValidationMessage>();
            pd.Validate(msgs);
            foreach (ValidationMessage vm in msgs)
            {
                if (vm.IsError) throw new Exception("Process definition invalid: " + vm.Message);
            }
            lock (this)
            {
                using (SoodaTransaction st = new SoodaTransaction())
                {
                    ProcessDefinitionDb t = FindProcessDefinitionInternal(pd.Name, pd.Version);
                    if (t != null) throw new Exception(string.Format("Process definition {0}.{1} already exists", pd.Name, pd.Version));
                    ProcessDefinitionDb pdb = new ProcessDefinitionDb();
                    pdb.Name = pd.Name;
                    pdb.ProcessXml = definitionXml;
                    pdb.Version = pd.Version;
                    st.Commit();
                    _pdCache.Remove(pdb.Id.ToString());
                }
            }
        }

        public string GetProcessDefinitionId(string name, int version)
        {
            using (SoodaTransaction st = new SoodaTransaction())
            {
                ProcessDefinitionDb pd = FindProcessDefinitionInternal(name, version);
                return pd.Id.ToString();
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Repository for storing process definitions
    /// </summary>
    public interface IProcessDefinitionRepository
    {
        string InsertProcessDefinition(ProcessDefinition pd);
        void UpdateProcessDefinition(ProcessDefinition pd);
        string GetProcessDefinitionId(string name, int version);
        void DeleteProcessDefinition(string definitionId);
        ProcessDefinition GetProcessDefinition(string name, int version);
        ProcessDefinition GetProcessDefinition(string definitionId);
        IList<string> GetProcessDefinitionNames();
        IList<int> GetProcessDefinitionVersions(string name);
        IList<string> GetProcessDefinitionIds();
    }
}

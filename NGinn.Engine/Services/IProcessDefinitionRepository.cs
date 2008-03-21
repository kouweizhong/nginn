using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using System.IO;
namespace NGinn.Engine.Services
{
    /// <summary>
    /// Repository for storing process definitions
    /// </summary>
    public interface IProcessDefinitionRepository
    {
        string InsertProcessDefinition(string definitionXml);
        void UpdateProcessDefinition(string definitionXml);
        void DeleteProcessDefinition(string definitionId);
        string GetProcessDefinitionId(string name, int version);
        ProcessDefinition GetProcessDefinition(string definitionId);
    }
}

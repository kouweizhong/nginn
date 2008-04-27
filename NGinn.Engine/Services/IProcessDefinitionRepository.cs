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
        ProcessDefinition GetProcessDefinition(string definitionId);
        string GetProcessDefinitionId(string packageName, string processName, int version);
        /// <summary>
        /// Retrun XML schema for process input data
        /// </summary>
        /// <param name="definitionId"></param>
        /// <returns></returns>
        string GetProcessInputSchema(string definitionId);
    }

    
}

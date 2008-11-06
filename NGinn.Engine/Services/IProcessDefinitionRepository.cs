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
        /// Returns specified schema
        /// </summary>
        /// <param name="definitionId">process definition id or package name</param>
        /// <param name="schemaName">name of package schema file to return or
        /// - 'input' for process input schema
        /// </param>
        /// <returns></returns>
        string GetPackageSchema(string definitionId, string schemaName);
    }

    
}

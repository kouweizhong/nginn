using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Process script manager interface. Process script manager delivers
    /// script execution engine for evaluating script expressions in process
    /// and tasks. 
    /// </summary>
    public interface IProcessScriptManager
    {
        /// <summary>
        /// Notify process script manager that process definition has been updated
        /// </summary>
        /// <param name="pd"></param>
        void ProcessDefinitionUpdated(ProcessDefinition pd);

        /// <summary>
        /// Notify process script manager that package definition has been updated.
        /// </summary>
        /// <param name="pd"></param>
        void PackageDefinitionUpdated(PackageDefinition pd);

        /// <summary>
        /// Get process script implementation
        /// </summary>
        /// <param name="pd"></param>
        /// <returns></returns>
        IProcessScript GetProcessScript(ProcessDefinition pd);

        /// <summary>
        /// Return task script implementation
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="taskId"></param>
        /// <returns></returns>
        ITaskScript GetTaskScript(ProcessDefinition pd, string taskId);

    }
}

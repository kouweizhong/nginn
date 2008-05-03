using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces
{
    /// <summary>
    /// Process hosting environment interface
    /// </summary>
    public interface INGEnvironment
    {
        /// <summary>
        /// Start new process instance
        /// </summary>
        /// <param name="definitionId">Process definition ID (check IProcessDefinitionRepository)</param>
        /// <param name="inputVariables">Process input variables</param>
        /// <returns>new process instance ID</returns>
        string StartProcessInstance(string definitionId, string inputXml);
        /// <summary>
        /// Get lists of processes that can be 'kicked', that is, have some work to do
        /// </summary>
        /// <returns>list of process instance IDs</returns>
        IList<string> GetKickableProcesses();

        /// <summary>
        /// Kick a process instance. If there is some work to do in process instance, execute some of that work
        /// </summary>
        /// <param name="instanceId">Process instance ID</param>
        void KickProcess(string instanceId);

        /// <summary>
        /// Set environment variable. Used for passing external information to process instances.
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">variable value</param>
        void SetEnvVariable(string name, object value);

        /// <summary>
        /// Return value of environment variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetEnvVariable(string name);


        /// <summary>
        /// Return xml document containing current process instance data
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        string GetProcessInstanceData(string instanceId);
    }
}

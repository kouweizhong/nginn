using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    [Serializable]
    public class WorkItem
    {
        ///task identifier in process definition
        public string TaskId;
        /// <summary>Process definition ID</summary>
        public string ProcessDefinitionId;
        /// <summary>Process instance id</summary>
        public string ProcessInstanceId;
        /// <summary>Correlation id used for reporting task completed</summary>
        public string CorrelationId;
        /// <summary>Work item title - as it will appear on TODO list</summary>
        public string Title;
        /// <summary>Work item description (body) - non mandatory</summary>
        public string Description;
        /// <summary></summary>
        public string AssigneeId;
        /// <summary></summary>
        public string AssigneeGroupId;
        /// <summary>True if task should be assigned to group, false - if it should be assigned to a person</summary>
        public bool IsGroupResponsible;
        /// <summary>In case of 'person' responsibility, if true system will automatically put the task in 'executing' state, otherwise it will be in 'assigned' state</summary>
        public bool ImplicitStartExecution;
        /// <summary>true if task assignee can give the task to another person, false if assignee cannot change person responsible</summary>
        public bool CanAssigneeDelegateTask;
        /// <summary>in case of group tasks, if false - group members can take the task from group queue, if true - group manager has to assign tasks to group members</summary>
        public bool ManagerAssignsTask;
        /// <summary>List of options for task result. If this list is empty or null, no task result code is required</summary>
        public IList<string> OptionsForResultCode;

        public IDictionary<string, object> InputVariables = new Dictionary<string, object>();
    }
    /// <summary>
    /// Interface for creating and tracking work items. It is used by workflow engine for assigning tasks to people.
    /// </summary>
    public interface IWorkListService
    {
        string CreateWorkItem(WorkItem wi);
    }
}

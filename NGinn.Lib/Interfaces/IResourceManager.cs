using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces
{
    /// <summary>
    /// Resource manager interface. Contains methods for selecting resources.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Select assignee from group
        /// </summary>
        /// <param name="groupId">Id of the group</param>
        /// <param name="excludedPersons">list of IDs of people to exclude in selection</param>
        /// <param name="selectionStrategy">selection strategy</param>
        /// <returns></returns>
        string SelectAssigneeFromGroup(string groupId, string[] excludedPersons, string selectionStrategy);
    }
}

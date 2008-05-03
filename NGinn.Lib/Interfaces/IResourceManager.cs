using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces
{
    public class WorkResource
    {
        public string Id;
    }

    public class Person : WorkResource
    {
    }

    public class OrgUnit : WorkResource
    {
    }

    /// <summary>
    /// Resource manager interface. Contains methods for selecting resources.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// How assignee selection is done
        /// a) by specifying person ID
        /// b) by specifying group ID
        /// c) by specifying process role ID
        /// In case of group or role, we can select a person from this group according to some criteria
        /// b.1) by excluding a list of persons
        /// b.2) by adding a selection strategy that will be used to choose the person. Supported selection strategies
        ///      - random
        ///      - min-queue (person with minimum number tasks in the TODO list)
        ///      - round-robin
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="exclusions">List of comma-separated ids of persons</param>
        /// <param name="selectionStrategy">Person selection strategy</param>
        /// <returns></returns>
        string SelectAssignee(string expression, string exclusions, string selectionStrategy);
    }
}

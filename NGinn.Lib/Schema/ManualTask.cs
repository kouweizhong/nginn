using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    public enum TaskAssignmentStrategy
    {
        PERSON,
        GROUP
    }



    [Serializable]
    public class ManualTask : Task
    {
        private string _name;
        private string _descrTemplate;
        private TaskAssignmentStrategy _assignmentStrategy = TaskAssignmentStrategy.PERSON;
        private string _assigneeQuery;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string DescriptionTemplate
        {
            get { return _descrTemplate; }
            set { _descrTemplate = value; }
        }

        public TaskAssignmentStrategy AssignmentStrategy
        {
            get { return _assignmentStrategy; }
            set { _assignmentStrategy = value; }
        }

        public override bool IsImmediate
        {
            get { return false; }
        }

        public enum InitialTaskStatus
        {
            Offered,
            Assigned
        }

        /// <summary>
        /// Query syntax: TBD
        /// </summary>
        public string TaskAssigneeQuery
        {
            get { return _assigneeQuery; }
            set { _assigneeQuery = value; }
        }

        public string ExcludeAssignees
        {
            get { return null; }
            set { }
        }

        public string AssigneeSelectionFunction
        {
            get { return null; }
        }


    }
}

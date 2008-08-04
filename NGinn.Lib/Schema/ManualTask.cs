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

        public string SelectionStrategy
        {
            get { return null; }
        }


        public override bool IsImmediate
        {
            get { return false; }
        }

        public string AssigneeGroup
        {
            get { return null; }
        }

        public string[] ExcludePeople
        {
            get { return null; }
        }

        public string Assignee
        {
            get { return null; }
        }

    }
}

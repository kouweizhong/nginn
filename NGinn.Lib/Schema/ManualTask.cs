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

        
    }
}

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
        

        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("AssigneeId", typeof(string), false, true, true),
                new TaskParameterInfo("AssigneeGroup", typeof(string), false, true, true),
                new TaskParameterInfo("Title", typeof(string), false, true, true),
                new TaskParameterInfo("Description", typeof(string), false, true, true),
            };
        }


    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class SubprocessTask: Task
    {
        public override TaskParameterInfo[] GetTaskParameters()
        {
            TaskParameterInfo[] pars = new TaskParameterInfo[] {
                new TaskParameterInfo("ProcessDefinitionId", typeof(string), true, true, true),
            };
            return pars;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class SubprocessTask: Task
    {
        public string SubprocessDefinitionId;

        public override bool IsImmediate
        {
            get { return false; }
        }
    }
}

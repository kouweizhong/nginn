using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;

namespace NGinn.Engine.Runtime.Tasks
{
    [Serializable]
    public class EmptyTaskActive : ActiveTransition
    {
        public EmptyTaskActive(EmptyTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {

        }
    }

}

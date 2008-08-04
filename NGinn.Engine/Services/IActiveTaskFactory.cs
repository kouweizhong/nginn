using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Runtime;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Services
{
    public interface IActiveTaskFactory
    {
        IActiveTask CreateActiveTask(Task tsk);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// TODO: Make factory class configurable through script.net
    /// </summary>
    internal class ActiveTaskFactory : IActiveTaskFactory
    {
        public IActiveTask CreateActiveTask(Task tsk)
        {
               
            IActiveTask at;
            if (tsk is EmptyTask)
            {
                at = new EmptyTaskActive(tsk);
            }
            else if (tsk is TimerTask)
            {
                at = new TimerTaskActive(tsk);
            }
            else if (tsk is ScriptTask)
            {
                at = new ScriptTaskActive((ScriptTask)tsk);
            }
            else if (tsk is NotificationTask)
            {
                at = new NotificationTaskActive((NotificationTask)tsk);
            }
            else if (tsk is ReceiveMessageTask)
            {
                at = new ReceiveMessageTaskActive(tsk);
            }
            else if (tsk is ManualTask)
            {
                at = new ManualTaskActive((ManualTask)tsk);
            }
            else throw new Exception();
            return at;
        }
    }
}

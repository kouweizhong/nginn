using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// MultiInstanceTask handles multiple instance tasks, that is tasks that can be instantiated
    /// in more than one instance. It wraps several instances of a task.
    /// </summary>
    [Serializable]
    public class MultiInstanceTask : ActiveTransition
    {
        private List<ActiveTransition> _instances = new List<ActiveTransition>();

        public MultiInstanceTask(Task tsk, ProcessInstance pi)
            : base(tsk, pi)
        {

        }

        public IList<ActiveTransition> TaskInstances
        {
            get { return _instances; }
        }

        public override void CancelTask()
        {
            foreach (ActiveTransition at in TaskInstances)
            {
                at.CancelTask();
            }
            base.CancelTask();
        }

        public override bool IsImmediate
        {
            get
            {
                return this.ProcessTask.IsImmediate;
            }
        }

        public override void Activate()
        {
            foreach (ActiveTransition at in TaskInstances)
            {
                at.Activate();
            }
            base.Activate();
        }

        public override void Passivate()
        {
            foreach (ActiveTransition at in TaskInstances)
            {
                at.Passivate();
            }
            base.Passivate();
        }

        
    }
}

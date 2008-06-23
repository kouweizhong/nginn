using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Represents multi-instance transition. This class holds several
    /// instances of transitions
    /// </summary>
    [Serializable]
    public class MultiInstanceTaskActive : ActiveTransition, ITransitionCallback
    {
        private List<string> _transitions = new List<string>();

        public MultiInstanceTaskActive(Task tsk, ProcessInstance pi)
            : base(tsk, pi)
        {
        }

        #region ITransitionCallback Members

        public void TransitionEnabled(string correlationId)
        {
            throw new NotImplementedException();
        }

        public void TransitionStarted(string correlationId)
        {
            throw new NotImplementedException();
        }

        public void TransitionCompleted(string correlationId)
        {
            throw new NotImplementedException();
        }

        public void TransitionCancelled(string correlationId)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected override void DoCancelTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoExecuteTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoInitiateTask()
        {
            throw new NotImplementedException();
        }
    }
}

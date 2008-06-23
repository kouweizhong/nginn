using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Interface for reporting transition status 
    /// to transition container (process instance or multi-instance task)
    /// </summary>
    public interface ITransitionCallback
    {
        /// <summary>
        /// Called after the transition has been started
        /// This is optional callback. 
        /// </summary>
        /// <param name="correlationId"></param>
        void TransitionStarted(string correlationId);
        /// <summary>
        /// Called after the transition has been completed
        /// </summary>
        /// <param name="correlationId"></param>
        void TransitionCompleted(string correlationId);
    }
}

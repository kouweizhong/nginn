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
        void TransitionEnabled(string correlationId);
        void TransitionStarted(string correlationId);
        void TransitionCompleted(string correlationId);
        void TransitionCancelled(string correlationId);
    }
}

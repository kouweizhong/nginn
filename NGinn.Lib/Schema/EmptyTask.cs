using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Empty task performs no action, is can be used for synchronization or routing
    ///	without any side-effects.
    /// </summary>
    [Serializable]
    public class EmptyTask : Task
    {
        public override bool IsImmediate
        {
            get { return true; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;

namespace NGinn.Lib.Interfaces
{
    /// <summary>
    /// Interface for handling task persistence.
    /// If task (active task) implements this interface
    /// NGinn will use it for persisting the task instance.
    /// Otherwise it will use standard .Net serialization.
    /// Important: Immediate tasks will not be serialized at all.
    /// </summary>
    public interface INGinnPersistent
    {
        /// <summary>
        /// Save object's state in a DataObject record
        /// </summary>
        /// <returns></returns>
        DataObject SaveState();

        /// <summary>
        /// Restore object's state from a DataObject record.
        /// </summary>
        /// <param name="dob"></param>
        void RestoreState(DataObject dob);
    }
}

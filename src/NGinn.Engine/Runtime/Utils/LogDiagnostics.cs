using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NLog;

namespace NGinn.Engine.Runtime.Utils
{
    /// <summary>
    /// System diagnostics implementation - simple version
    /// </summary>
    public class LogDiagnostics : ISystemDiagnostics
    {
        private Logger log = LogManager.GetLogger("NGinn");

        #region ISystemDiagnostics Members

        public void Error(string msg)
        {
            log.Error("{0}", msg);
        }

        public void Error(string msg, Exception ex)
        {
            log.Error("{0}: {1}", msg, ex);
        }

        public void Warning(string msg)
        {
            log.Warn("{0}", msg);
        }

        public void Info(string msg)
        {
            log.Info("{0}", msg);
        }

        #endregion
    }
}

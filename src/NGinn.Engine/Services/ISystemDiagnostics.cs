using System;
using System.Collections;
using System.Text;

namespace NGinn.Engine.Services
{
    /// <summary>
    /// Diagnostic interface for NGinn
    /// </summary>
    public interface ISystemDiagnostics
    {
        void Error(string msg);
        void Error(string msg, Exception ex);
        void Warning(string msg);
        void Info(string msg);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services.Events
{
    /// <summary>
    /// Event used in system diagnostics.
    /// Application components send it to notify about their status.
    /// </summary>
    [Serializable]
    public class DiagnosticEvent
    {
        public enum EventSeverity
        {
            Info,
            Warn,
            Error
        }

        public string Message;
        public string Category;
        public string ErrorInfo;
        public DateTime Timestamp = DateTime.Now;
        public EventSeverity Severity;

        public DiagnosticEvent(EventSeverity severity, string category, string message)
        {
            Message = message;
            Category = category;
            Severity = severity;
        }

        public DiagnosticEvent(string category, Exception error)
        {
            Severity = EventSeverity.Error;
            Message = error.Message;
            ErrorInfo = error.ToString();
            Category = category;
        }
    }
}

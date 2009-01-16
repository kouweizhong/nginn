using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using System.Diagnostics;
using System.IO;

namespace NGinn.Utilities.Email
{
    [Serializable]
    public class EmailMsgOut
    {
        public string CorrelationId;
        public string Recipients;
        public string Cc;
        public string Bcc;
        public string Subject;
        public string Body;
    }

    public class SMTPEmailSender
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private string _exePath;
        private string _tempDir;
        private string _cmdLine = "";

        public string TempDir
        {
            get { return _tempDir; }
            set { _tempDir = value; }
        }

        public string ExePath
        {
            get { return _exePath; }
            set { _exePath = value; }
        }

        public string CmdLine
        {
            get { return _cmdLine; }
            set { _cmdLine = value; }
        }

        public void SendMessage(EmailMsgOut msg)
        {
            lock (this)
            {
                if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
            }
            string fn = Guid.NewGuid().ToString("N") + ".txt";
            string bodyFile = Path.Combine(TempDir, fn);
            using (StreamWriter sw = new StreamWriter(bodyFile, false, Encoding.UTF8))
            {
                sw.Write(msg.Body);
            }
            string arguments = string.Format("\"{0}\" -to \"{1}\" -subject \"{2}\" {3}", bodyFile, msg.Recipients, msg.Subject, CmdLine);
            log.Info("Blat commandline: {0} {1}", ExePath, arguments);
            ProcessStartInfo psi = new ProcessStartInfo(ExePath, arguments);
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                log.Info("Email sent");
            }
        }
    }
}

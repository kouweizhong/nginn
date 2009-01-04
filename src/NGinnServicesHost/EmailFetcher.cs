using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NLog;
using System.IO;
using NGinn.Lib.Interfaces.MessageBus;
using NGinn.Utilities.Email;


namespace NGinnServicesHost
{
    public class EmailFetcher
    {
        private Logger log = LogManager.GetCurrentClassLogger();

        public EmailFetcher()
        {
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public void Run()
        {
            try
            {
                if (!Directory.Exists(BaseDirectory)) Directory.CreateDirectory(BaseDirectory);
                Fetch();
            }
            catch (Exception ex)
            {
                log.Error("Error: {0}", ex);
                throw;
            }
        }

        protected void Fetch()
        {
            if (FetcherExe == null || FetcherExe.Length == 0) return;
            if (CommandLine == null || CommandLine.Length == 0) throw new Exception();
            string cmd = string.Format("{0} /outdir \"{1}\"", CommandLine, BaseDirectory);
            log.Debug("Exec {0} {1}", FetcherExe, cmd);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = FetcherExe;
            psi.Arguments = cmd;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            Process p = Process.Start(psi);
            p.WaitForExit();
        
        }

        

        private string _cmdLine;
        public string CommandLine
        {
            get { return _cmdLine; }
            set { _cmdLine = value; }
        }

        private string _baseDir;
        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }

        private string _fetcherExe;
        public string FetcherExe
        {
            get { return _fetcherExe; }
            set { _fetcherExe = value; }
        }

        private IMessageBus _outMsgBus;
        public IMessageBus TargetMessageBus
        {
            get { return _outMsgBus; }
            set { _outMsgBus = value; }
        }

    }
}

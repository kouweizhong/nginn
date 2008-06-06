using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using Spring.Context;
using NLog;
using NGinn.Lib.Interfaces;
using NGinn.Engine.Runtime;
using System.IO;

namespace NGinn.Engine.Host
{
    public partial class NGinnHostService : ServiceBase
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private NGEngine _eng = null;

        public NGinnHostService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.Start();
        }

        protected override void OnStop()
        {
            lock (this)
            {
                log.Debug("Stopping the NGinn engine");
                _eng.Stop();
                log.Debug("NGinn engine stopped");
            }
        }

        public void Start()
        {
            lock (this)
            {
                try
                {
                    string path = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                    log.Debug("Configuring remoting from file: {0}", path);
                    System.Runtime.Remoting.RemotingConfiguration.Configure(path, false);
                    IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
                    _eng = (NGEngine)ctx.GetObject("NGinn.Engine", typeof(NGEngine));
                    log.Debug("Starting the NGinn engine");
                    _eng.Start();
                    log.Debug("NGinn engine started");
                }
                catch (Exception ex)
                {
                    log.Error("Error starting engine: {0}", ex);
                    throw;
                }
            }
        }
    }
}

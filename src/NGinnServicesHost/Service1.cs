using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using NLog;
using Spring.Context;

namespace NGinnServicesHost
{
    public partial class Service1 : ServiceBase
    {
        private Logger log = LogManager.GetCurrentClassLogger();

        public Service1()
        {
            InitializeComponent();
        }

        protected void InitializeComponent()
        {
            this.ServiceName = "NGinnServiceHost";
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
        }

        public void Start()
        {
            try
            {
                IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            }
            catch (Exception ex)
            {
                log.Error("Error: {0}", ex);
                throw;
            }
        }
    }
}

using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System;

namespace NGinn.Engine.Host
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Debug();
                return;
            }
            ServiceBase[] ServicesToRun;

            // More than one user Service may run within the same process. To add
            // another service to this process, change the following line to
            // create a second service object. For example,
            //
            //   ServicesToRun = new ServiceBase[] {new Service1(), new MySecondUserService()};
            //
            ServicesToRun = new ServiceBase[] { new NGinnHostService() };

            ServiceBase.Run(ServicesToRun);
        }

        static void Debug()
        {
            try
            {
                NGinnHostService srv = new NGinnHostService();
                srv.Start();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Error: {0}", ex);
            }
            System.Console.ReadLine();
        }
    }
}
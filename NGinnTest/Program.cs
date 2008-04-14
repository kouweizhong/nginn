using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using Spring.Context;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;
using Sooda;
using System.IO;

namespace NGinnTest
{
    class Program
    {
        static Logger log = LogManager.GetCurrentClassLogger();
        static IApplicationContext _ctx;

        static void Main(string[] args)
        {
            NLog.Config.SimpleConfigurator.ConfigureForConsoleLogging(LogLevel.Debug);
            try
            {
                _ctx = Spring.Context.Support.ContextRegistry.GetContext();
                //TestProcessLoad();
                //TestDefinitionRepository();
                //TestKickProcess();
                //TestStartProcess();
                //TestTaskCompleted("a614a6b8617345a8b99e9805adcf1868", "a614a6b8617345a8b99e9805adcf1868.2");
                //TestTaskSelected("a614a6b8617345a8b99e9805adcf1868", "a614a6b8617345a8b99e9805adcf1868.2");
                TestPackageRepository();
            }
            catch (Exception ex)
            {
                log.Error("Error: {0}", ex);
            }
        }

        static void TestProcessLoad()
        {
            string pdName = "TestProcess2.xml";
            ProcessDefinition pd = new ProcessDefinition();
            log.Info("Loading process definition: {0}", pdName);
            pd.LoadXmlFile(pdName);
            log.Info("Process definition loaded: {0}.{1}", pd.Name, pd.Version);
            string schema = pd.GenerateInputSchema();
            log.Info("Process input data schema: {0}", schema);
        }

        static void TestDefinitionRepository()
        {
            string pdName = "TestProcess2.xml";
            ProcessDefinition pd = new ProcessDefinition();
            log.Info("Loading process definition: {0}", pdName);
            pd.LoadXmlFile(pdName);
            log.Info("Process definition loaded: {0}.{1}", pd.Name, pd.Version);
            IProcessDefinitionRepository rep = (IProcessDefinitionRepository) _ctx.GetObject("ProcessDefinitionRepository", typeof(IProcessDefinitionRepository));
            log.Info("Storing process definition in repository");
            using (StreamReader fs = new StreamReader(pdName))
            {
                string ret = rep.InsertProcessDefinition(fs.ReadToEnd());
                log.Info("Process definition stored with id={0}", ret);
            }
        }

        static void TestStartProcess()
        {
            INGEnvironment env = (INGEnvironment)_ctx.GetObject("NGEnvironment");
            IProcessDefinitionRepository pdr = (IProcessDefinitionRepository)_ctx.GetObject("ProcessDefinitionRepository");
            string id = pdr.GetProcessDefinitionId("Test_Process_3", 1);
            Dictionary<string, object> vars = new Dictionary<string, object>();
            vars["parent"] = 12343;
            string xml = string.Format("<data><parent>1234</parent></data>");
            string instId = env.StartProcessInstance(id, xml);
        }

        static void TestKickProcess()
        {
            INGEnvironment env = (INGEnvironment)_ctx.GetObject("NGEnvironment");
            IList<string> lst = env.GetKickableProcesses();
            if (lst.Count > 0)
            {
                env.KickProcess(lst[0]);
            }
        }

        static void TestTaskCompleted(string processId, string taskCorrelationId)
        {
            INGEnvironmentProcessCommunication env = (INGEnvironmentProcessCommunication)_ctx.GetObject("NGEnvironment");
            TaskCompletionInfo tci = new TaskCompletionInfo();
            tci.ProcessInstance = processId;
            tci.CorrelationId = taskCorrelationId;
            tci.ResultCode = "OK";
            tci.CompletedBy = "me";
            env.ProcessTaskCompleted(tci);
        }

        static void TestTaskSelected(string processId, string taskCorrelationId)
        {
            INGEnvironmentProcessCommunication env = (INGEnvironmentProcessCommunication)_ctx.GetObject("NGEnvironment");
            env.ProcessTaskSelectedForProcessing(processId, taskCorrelationId);
        }

        static void TestPackageRepository()
        {
            IProcessPackageRepository ppr = (IProcessPackageRepository) _ctx.GetObject("PackageRepository");
            foreach (string pkg in ppr.PackageNames)
            {
            };
        }
    }
}

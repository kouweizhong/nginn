using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using Spring.Context;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;
using Sooda;
using System.IO;
using System.Xml;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;

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
                System.Runtime.Remoting.RemotingConfiguration.Configure("NGinnTest.exe.config");
                _ctx = Spring.Context.Support.ContextRegistry.GetContext();
                //TestProcessLoad();
                //TestDefinitionRepository();
                //TestKickProcess("4f479a9e93964baaa2ce89e3960e263b");
                //TestStartProcess();
                TestTaskCompleted("4f479a9e93964baaa2ce89e3960e263b", "4f479a9e93964baaa2ce89e3960e263b.0");
                //TestTaskSelected("a614a6b8617345a8b99e9805adcf1868", "a614a6b8617345a8b99e9805adcf1868.2");
                //TestPackageRepository();
                //TestGetInstanceData("dca6f3bf215241f093b4baddc79d7c3e");
                //TransferDataTest();
                //DataTest();
                //ScriptTest.Test1();
                //ScriptTest.EvalTest6();
                //ValidationTest();
            }
            catch (Exception ex)
            {
                log.Error("Error: {0}", ex);
            }
            Console.WriteLine("Enter...");
            Console.ReadLine();
        }

        static void TestProcessLoad()
        {
            string pdName = "TestProcess2.xml";
            ProcessDefinition pd = new ProcessDefinition();
            log.Info("Loading process definition: {0}", pdName);
            pd.LoadFile(pdName);
            log.Info("Process definition loaded: {0}.{1}", pd.Name, pd.Version);
            string schema = pd.GetProcessInputXmlSchema();
            log.Info("Process input data schema: {0}", schema);
        }

        static void TestDefinitionRepository()
        {
            /*
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
            */
        }

        static void TestStartProcess()
        {
            INGEnvironment env = (INGEnvironment)_ctx.GetObject("NGEnvironment");
            IProcessDefinitionRepository pdr = (IProcessDefinitionRepository)_ctx.GetObject("ProcessDefinitionRepository");
            string id = pdr.GetProcessDefinitionId("TestPackage3", "Test_Process_3", 1);
            Dictionary<string, object> vars = new Dictionary<string, object>();
            vars["parent"] = 12343;
            string xml = string.Format("<Test_Process_3><ala>ma kota</ala><parent>123</parent></Test_Process_3>");
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


        static void TestKickProcess(string instId)
        {
            INGEnvironment env = (INGEnvironment)_ctx.GetObject("NGEnvironment");
            env.KickProcess(instId);
        }

        static void TestTaskCompleted(string processId, string taskCorrelationId)
        {
            INGEnvironmentProcessCommunication env = (INGEnvironmentProcessCommunication)_ctx.GetObject("NGEnvironment");
            TaskCompletionInfo tci = new TaskCompletionInfo();
            tci.ProcessInstance = processId;
            tci.CorrelationId = taskCorrelationId;
            tci.ResultXml = "<results><decision>Accept</decision><managerComment>I don't know why</managerComment><managerId>132</managerId></results>";
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

        static void TestGetInstanceData(string instanceId)
        {
            INGEnvironment env = (INGEnvironment)_ctx.GetObject("NGEnvironment");
            string xml = env.GetProcessInstanceData(instanceId);
            log.Info(xml);
            using (StreamWriter sw = new StreamWriter("instancedata.xml"))
            {
                sw.Write(xml);
            }
        }


        static void TransferDataTest()
        {
            /*
            string inputFile = "inputdata.xml";
            string targetFile = "targetdata.xml";
            List<VariableDef> vars = new List<VariableDef>();
            List<VariableBinding> bindings = new List<VariableBinding>();

            vars.Add(new VariableDef("Parent", "xs:int", VariableDef.Dir.InOut, VariableDef.Usage.Required, false));
            vars.Add(new VariableDef("OperatorName", "xs:string", VariableDef.Dir.In, VariableDef.Usage.Required, false));
            vars.Add(new VariableDef("Result", "xs:string", VariableDef.Dir.Out, VariableDef.Usage.Required, false));
            vars.Add(new VariableDef("Option", "xs:string", VariableDef.Dir.Local, VariableDef.Usage.Optional, true));

            bindings.Add(new VariableBinding("Parent", VariableBinding.VarBindingType.CopyVar, "TaskParent"));
            bindings.Add(new VariableBinding("Result", VariableBinding.VarBindingType.Xslt, "<xsl:for-each select='TaskResult'><Result><xsl:value-of select='.' /></Result></xsl:for-each>"));

            XmlDocument doc = new XmlDocument();
            doc.Load(inputFile);

            XmlDocument targetDoc = new XmlDocument();
            targetDoc.Load(targetFile);

            XmlElement el2 = XmlTest.TransferData(doc.DocumentElement, vars, bindings, targetDoc.DocumentElement);
            targetDoc.ReplaceChild(el2, targetDoc.DocumentElement);
            log.Info("Transfer results: {0}", targetDoc.OuterXml);
            */
        }

        static void DataTest()
        {
            DataObject dob = new DataObject();
            dob["ala"] = "ma kota";
            dob["jola"] = "nie ma zielonego pojêcia";
            dob["zenek"] = 35;
            dob["dziœ"] = DateTime.Now;
            dob["ala"] = "ju¿ nie ma kota";
            dob["kot"] = new List<string>(new string[] { "skarpeta", "tiger", "filemon", "kuba" });
            StringBuilder sb = new StringBuilder();

            foreach (string k in dob.Keys)
            {
                log.Debug("{0}={1}", k, dob[k]);
            }
            DataObject dob2 = new DataObject((IDataObject) dob);

            XmlWriter xw = XmlWriter.Create(sb);
            dob2.ToXml("dob2", xw);
            xw.Flush();
            log.Debug("XML: {0}", sb.ToString());
            string xml = "<?xml version=\"1.0\" ?><data id=\"11\">   <member1>aaa</member1> <member1>bbb</member1>  <and>\n<otherData>tutaj</otherData>\n<more>...</more></and>\n<member3>\nsome\ntext</member3>\n</data>";
            log.Debug("Parsing XML: {0}", xml);
            StringReader sr = new StringReader(xml);
            XmlReader xr = XmlReader.Create(sr);
            xr.MoveToContent();
            DataObject dob3 = DataObject.ParseXmlElement(xr);
            log.Debug("Result: {0}", dob3.ToXmlString("dob3"));
        }

        public static void ValidationTest()
        {
            TypeSet ts = new TypeSet();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ts;
            sd.Name = "T1";
            sd.Members.Add(new MemberDef("F1", "string", false, false));
            sd.Members.Add(new MemberDef("F2", "int", true, true));

            DataObject dob = new DataObject();
            dob["F1"] = 399;
            List<int> lst = new List<int>();
            lst.AddRange(new int[] {1, 2, 3, 4});
            dob["F2"] = lst;
            dob["F3"] = null;
            dob.Validate(sd);
        }
    }
}

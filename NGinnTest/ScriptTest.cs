using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
using NLog;
using ScriptNET;
using MutantFramework;
using Spring.Core;
using Spring.Expressions;
using System.Reflection;

namespace NGinnTest
{
    public class ScriptTest
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        /*
        public static void EvalTest2()
        {
            Evaluator.Evaluator ev = new Evaluator.Evaluator();
            //object obj = ev.Eval("({\"ala\": \"ma kota\", \"date\": \"" +  "})");
            
            DateTime st = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                //object obj = ev.Eval("({\"ala\": \"ma kota\", \"data\":\"" + i + "\"})");
                object obj = ev.Eval("1 + " + i);
            }
            log.Debug("Eval time: {0}", DateTime.Now - st);
        }
         */

        public static void EvalTest3()
        {
            DataObject dob = new DataObject();
            List<string> ls = new List<string>();
            ls.Add("ala");
            ls.Add("ma");
            ls.Add("kota");
            dob["data"] = ls;
            DataObject dob2 = new DataObject();
            dob2["dob"] = dob;
            Script scr = Script.Compile("eval('2 * 2');");
            DateTime st = DateTime.Now;
            string scrT = "2 * 2";
            for (int i = 0; i < 1000; i++)
            {
                //object v = dob2.GetValue("");
                scr.Execute();
                /*scr.AddObject("__inputStr", scrT);
                scr.AddObject("dob", dob);
                
                scr.ClearContext(ContextClearType.All);
                */
            }
            log.Info("1000 compilations: {0}", DateTime.Now - st);
        }

        public static void EvalTest()
        {
            DataObject dob = new DataObject();
            List<string> ls = new List<string>();
            ls.Add("ala");
            ls.Add("ma");
            ls.Add("kota");
            dob["data"] = ls;
            DataObject dob2 = new DataObject();
            dob2["dob"] = dob;
            Script scr = Script.Compile("eval(__inputStr);");
            DateTime st = DateTime.Now;
            string scrT = "2 * 2";
            DOBMutant dmu = new DOBMutant(dob2);
            
            for (int i = 0; i < 1000; i++)
            {
                scr.Context.SetItem("__inputStr", ContextItem.Variable, "dob2.dob.data[1]");
                scr.Context.SetItem("dob2", ContextItem.All, dmu);
                //object v = dob2.GetValue("");
                object v = scr.Execute();
                //log.Debug("V: " + v);
            }
            log.Info("1000 compilations: {0}", DateTime.Now - st);
        }

        public static void EvalTest4()
        {
            DataObject dob = new DataObject();
            List<string> ls = new List<string>();
            ls.Add("ala");
            ls.Add("ma");
            ls.Add("kota");
            dob["data"] = ls;
            DataObject dob2 = new DataObject();
            dob2["dob"] = dob;
            DOBMutant dmu = new DOBMutant(dob2);
            Script scr = Script.Compile("dob2.dob.data[1];");
            IScriptContext sc = scr.Context;
            //ScriptContext sc = new ScriptContext();
            log.Debug("Scope: {0}", sc.Scope.Name);
            sc.SetItem("dob2", ContextItem.Variable, dmu);

            object ret = scr.Execute();
            log.Debug("Result: {0}", ret);
        }

        public static void EvalTest5()
        {
            DataObject dob = new DataObject();
            dob["ala"] = "kot";
            DataObject dob2 = new DataObject();
            dob2["dob"] = dob;
            Mutant mut = new DOBMutant(dob2); // DataMutantConverter.ToMutant(dob2);
            //Script scr = Script.Compile("dob2.dob.ala;");
            //IScriptContext sc = scr.Context;
            IScriptContext sc = new ScriptContext();
            sc.SetItem("dob2", ContextItem.Variable, mut);
            //object ret = scr.Execute();
            object ret = Script.RunCode("dob2.dob.ala;", sc);
            log.Debug("Result: {0}", ret);
        }

        public static void EvalTest6()
        {
            DataObject dob = new DataObject();
            dob["ala"] = "Kot";
            Mutant mut = new DOBMutant(dob);
            IScriptContext ctx = new ScriptContext();
            ctx.SetItem("dob", ContextItem.Variable, mut);
            Script.RunCode("dob.kot = 'ma ale';", ctx);
            log.Debug("Kot: {0}", dob["kot"]);
            Script.RunCode("dob.ala = dob.kot;", ctx);
            log.Debug("Ala: {0}", dob["ala"]);
            object v = Script.RunCode("dob.kot;", ctx);
            log.Debug("RET: {0}", v);
        }

        public static void Test1()
        {
            DataObject dob = new DataObject();
            dob["ala"] = "ma kota";
            DataObject ob2 = new DataObject();
            ob2["data"] = DateTime.Now;
            dob["kot"] = ob2;
            log.Info("Data: {0}", dob.ToXmlString("dob"));
            DateTime st = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                Script scr1 = Script.Compile("dob.data = 'kot to zwierze';\nlog.Info('dob.kot.data={0}', dob.kot.data);");
            }
            log.Info("1000 compilations: {0}", DateTime.Now - st);
            Script scr = Script.Compile("dob.data = 'kot to zwierze';\nlog.Info('dob.kot.data={0}', dob.kot.data);");
            Mutant mut = DataMutantConverter.ToMutant(dob);
            
            
        }



        public static object GetValue(DataObject obj, string property)
        {
            object ret = obj.GetValue(property);
            //object ret = ExpressionEvaluator.GetValue(obj, property);
            log.Debug("Get {0}: {1}", property, ret);
            return ret;
        }

        public static void SetValue(object obj, string property, object newValue)
        {
            log.Debug("Set {0}={1}", property, newValue);
            ExpressionEvaluator.SetValue(obj, property, newValue);
        }

    }
}

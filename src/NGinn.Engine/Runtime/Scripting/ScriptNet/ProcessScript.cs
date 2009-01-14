using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;
using NLog;
using NGinn.Lib.Data;
using ScriptNET;

namespace NGinn.Engine.Runtime.Scripting.ScriptNet
{
    public class ProcessScript : IProcessScript
    {
        private ProcessDefinition _pd;
        private ScriptNetProcessScriptManager _mgr;
        private Logger log;

        public ProcessScript(ScriptNetProcessScriptManager mgr, ProcessDefinition pd)
        {
            _pd = pd;
            _mgr = mgr;
            log = LogManager.GetCurrentClassLogger();
        }

        private IDataObject _data;
        public NGinn.Lib.Data.IDataObject ProcessData
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        private ProcessInstance _pi;
        public ProcessInstance Instance
        {
            get { return _pi; }
            set { _pi = value; }
        }

        private INGEnvironmentContext _envCtx;
        public INGEnvironmentContext EnvironmentContext
        {
            get { return _envCtx; }
            set { _envCtx = value; }
        }

        private IScriptContext GetScriptContext()
        {
            ScriptContext sc = new ScriptContext();
            
            sc.SetVariable("data", new DOBMutant(ProcessData));
            sc.SetVariable("definition", _pd);
            sc.SetVariable("instance", _pi);
            sc.SetVariable("envCtx", EnvironmentContext);
            sc.SetVariable("log", log);
            return sc;
        }

        delegate string GetScriptDelegate();

        private object ExecuteScript(string scriptKey, GetScriptDelegate dlg)
        {
            Script scr = _mgr.GetCachedProcessScript(_pd.DefinitionId, scriptKey);
            if (scr == null)
            {
                lock (_mgr)
                {
                    scr = _mgr.GetCachedProcessScript(_pd.DefinitionId, scriptKey);
                    if (scr == null)
                    {
                        string sc = dlg();
                        if (sc == null) return null;
                        sc = sc.Trim();
                        if (!sc.EndsWith(";")) sc += ";";
                        scr = Script.Compile(sc);
                        
                        _mgr.SetCachedProcessScript(_pd.DefinitionId, scriptKey, scr);
                    }
                }
            }
            scr.Context = GetScriptContext();
            return scr.Execute();
        }

        public object GetDefaultVariableValue(string variableName)
        {
            string key = "DefaultVariableValue_" + variableName;
            return ExecuteScript(key, delegate()
            {
                foreach (VariableDef vd in _pd.ProcessVariables)
                {
                    if (vd.Name == variableName)
                        return vd.DefaultValueExpr;
                }
                return null;
            });
        }


        public object EvaluateFlowInputCondition(Flow fl)
        {
            string key = string.Format("Flow_{0}_{1}", fl.From.Id, fl.To.Id);

            return ExecuteScript(key, delegate()
            {
                return fl.InputCondition;
            });
        }
    }
}

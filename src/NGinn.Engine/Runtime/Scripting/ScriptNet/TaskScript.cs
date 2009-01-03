using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Lib.Data;
using NGinn.Lib.Schema;
using NGinn.Engine.Runtime;
using NLog;
using ScriptNET;

namespace NGinn.Engine.Runtime.Scripting.ScriptNet
{
    public class TaskScript : ITaskScript
    {
        private Logger log;
        private Task _taskDef;
        private ScriptNetProcessScriptManager _mgr;

        public TaskScript(ScriptNetProcessScriptManager mgr, Task taskDef)
        {
            _taskDef = taskDef;
            _mgr = mgr;
            log = LogManager.GetLogger("TaskScript");
        }

        #region ITaskScript Members
        private IDataObject _data;
        public NGinn.Lib.Data.IDataObject SourceData
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

        private IActiveTaskContext _taskCtx;
        public IActiveTaskContext TaskContext
        {
            get
            {
                return _taskCtx;
            }
            set
            {
                _taskCtx = value;
            }
        }

        private IActiveTask _task;
        public IActiveTask TaskInstance
        {
            get
            {
                return _task;
            }
            set
            {
                _task = value;
            }
        }

        private IScriptContext GetScriptContext()
        {
            ScriptContext sc = new ScriptContext();
            sc.SetItem("data",  ContextItem.Variable, new DOBMutant(SourceData));
            sc.SetItem("task", ContextItem.Variable, TaskInstance);
            sc.SetItem("taskDef", ContextItem.Variable, _taskDef);
            sc.SetItem("log", ContextItem.Variable, log);
            return sc;
        }

        delegate string GetScriptDelegate();

        private object ExecuteScript(string scriptKey, GetScriptDelegate dlg)
        {
            log.Debug("Executing script {0}.{1}", _taskDef.ParentProcess.DefinitionId, scriptKey);
            Script scr = _mgr.GetCachedProcessScript(_taskDef.ParentProcess.DefinitionId, scriptKey);
            if (scr == null)
            {
                lock (_mgr)
                {
                    scr = _mgr.GetCachedProcessScript(_taskDef.ParentProcess.DefinitionId, scriptKey);
                    if (scr == null)
                    {
                        string sc = dlg();
                        log.Debug("Script {0}.{1}: compiling script body: {2}", _taskDef.ParentProcess.DefinitionId, scriptKey, sc);
                        if (sc == null) return null;
                        sc = sc.Trim();
                        if (!sc.EndsWith(";")) sc += ";";
                        scr = Script.Compile(sc);
                        _mgr.SetCachedProcessScript(_taskDef.ParentProcess.DefinitionId, scriptKey, scr);
                    }
                }
            }
            scr.Context = GetScriptContext();
            return scr.Execute();
        }

        public object GetInputParameterValue(string paramName)
        {
            TaskParameterBinding tb = null;
            foreach (TaskParameterBinding tbi in _taskDef.ParameterBindings)
            {
                if (tbi.PropertyName == paramName)
                {
                    tb = tbi; break;
                }
            }
            if (tb == null) throw new ApplicationException("No binding for parameter: " + paramName);

            string key = string.Format("Task_{0}_InputParameter_{1}", _taskDef.Id, tb.PropertyName);

            return ExecuteScript(key, delegate()
            {
                return tb.BindingExpression;
            });
        }

        public object GetDefaultVariableValue(string variableName)
        {
            string key = string.Format("Task_{0}_DefaultValue_{1}", _taskDef.Id, variableName);

            return ExecuteScript(key, delegate() {
                foreach(VariableDef vd in _taskDef.TaskVariables)
                {
                    if (vd.Name == variableName) return vd.DefaultValueExpr;
                }
                return null;
            });

        }

        public object RunScriptBlock(string blockId)
        {
            string key = string.Format("Task_{0}_ScriptBlock_{1}", _taskDef.Id, blockId);

            return ExecuteScript(key, delegate()
            {
                if (_taskDef is ScriptTask && blockId == "ScriptBody")
                {
                    foreach(TaskParameterBinding tbi in _taskDef.ParameterBindings)
                    {
                        if (tbi.PropertyName == "ScriptBody")
                            return tbi.BindingExpression;
                    }
                }
                return null;
            });
        }

        public object EvalInputVariableBinding(string varName)
        {
            string key = string.Format("Task_{0}_InputVariable_{1}", _taskDef.Id, varName);

            return ExecuteScript(key, delegate()
            {
                foreach (VariableBinding vb in _taskDef.InputBindings)
                {
                    if (vb.VariableName == varName) return vb.BindingExpression;
                }
                return null;
            });
        }

        public object EvalOutputVariableBinding(string varName)
        {
            string key = string.Format("Task_{0}_OutputVariable_{1}", _taskDef.Id, varName);

            return ExecuteScript(key, delegate()
            {
                foreach (VariableBinding vb in _taskDef.OutputBindings)
                {
                    if (vb.VariableName == varName) return vb.BindingExpression;
                }
                return null;
            });
        }

        public object EvalMultiInstanceSplitQuery()
        {
            string key = string.Format("Task_{0}_SplitQuery", _taskDef.Id);

            return ExecuteScript(key, delegate()
            {
                return _taskDef.MultiInstanceSplitQuery;
            });
        }
        #endregion
    }
}

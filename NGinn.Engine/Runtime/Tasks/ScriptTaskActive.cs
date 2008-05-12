using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using ScriptNET;
using NLog;

namespace NGinn.Engine.Runtime.Tasks
{
    [Serializable]
    public class ScriptTaskActive : ActiveTransition
    {
        public ScriptTaskActive(ScriptTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {
        }

        public override bool IsImmediate
        {
            get
            {
                return true;
            }
        }

        public override void ExecuteTask()
        {
            base.ExecuteTask();
            ScriptTask st = (ScriptTask)ProcessTask;
            IScriptContext ctx = CreateTaskScriptContext();
            log.Debug("Executing script in task {0}", st.Id);
            Script.RunCode(st.Script, ctx);
            log.Debug("Script executed");
        }
    }
}

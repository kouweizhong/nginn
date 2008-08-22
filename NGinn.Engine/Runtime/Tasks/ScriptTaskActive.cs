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
    public class ScriptTaskActive : ActiveTaskBase
    {
        private string _script;

        public ScriptTaskActive(ScriptTask tsk)
            : base(tsk)
        {
        }

        public override bool IsImmediate
        {
            get
            {
                return true;
            }
        }

        [TaskParameter(IsInput=true, Required=true, DynamicAllowed=false)]
        public string ScriptBody
        {
            get { return _script; }
            set { _script = value; }
        }

        public override void CancelTask()
        {
            throw new NotImplementedException();
        }

        protected override void DoInitiateTask()
        {
            try
            {
                IScriptContext ctx = this.CreateScriptContext(VariablesContainer);
                string code = ScriptBody.Trim();
                if (!code.EndsWith(";")) code += ";";
                Script.RunCode(code, ctx);
                OnTaskCompleted();
            }
            catch (Exception ex)
            {
                log.Error("Error executing script {0}: {1}", ScriptBody, ex);
                throw;
            }
        }


    }
}

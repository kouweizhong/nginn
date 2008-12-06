using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using NGinn.Engine.Services;

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
                ITaskScript scr = CreateScriptContext(VariablesContainer);
                scr.RunScriptBlock("ScriptBody");
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

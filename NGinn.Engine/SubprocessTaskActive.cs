using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using Spring.Context;
using NGinn.Engine.Services;
using NLog;

namespace NGinn.Engine
{
    [Serializable]
    class SubprocessTaskActive : ActiveTransition
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private string _subprocessInstanceId;
        [NonSerialized]
        private SubprocessTask _task;

        public SubprocessTaskActive(SubprocessTask tsk, ProcessInstance pi)
            : base(tsk, pi)
        {
            _subprocessInstanceId = tsk.SubprocessDefinitionId;
        }

        public override void Activate()
        {
            base.Activate();
            _task = (SubprocessTask) _processInstance.Definition.GetTask(TaskId);
        }

        
        public override void InitiateTask()
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment", typeof(INGEnvironment));
            IDictionary<string, object> inputVars = new Dictionary<string, object>();
            inputVars["_NGinn_ParentProcess"] = this.ProcessInstanceId;
            log.Info("Starting subprocess {0}", _task.SubprocessDefinitionId);
            string xml = string.Format("<data><var1>ala</var1><var2>{0}</var2></data>", DateTime.Now);
            string id = env.StartProcessInstance(_task.SubprocessDefinitionId, xml);
            log.Info("Process started: Instance ID={0}", id);
            this._subprocessInstanceId = id;
            this.Status = TransitionStatus.ENABLED;
        }

    }
}

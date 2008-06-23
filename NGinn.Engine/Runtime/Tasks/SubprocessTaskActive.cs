using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using Spring.Context;
using NGinn.Engine.Services;
using NLog;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;

namespace NGinn.Engine.Runtime.Tasks
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

        protected override void DoInitiateTask()
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment", typeof(INGEnvironment));
            IDictionary<string, object> inputVars = new Dictionary<string, object>();
            inputVars["_NGinn_ParentProcess"] = this.ProcessInstanceId;
            log.Info("Starting subprocess {0}", _task.SubprocessDefinitionId);
            IDataObject dob = GetTaskData();
            string xml = dob.ToXmlString("data");
            string id = env.StartProcessInstance(_task.SubprocessDefinitionId, xml, GetSubprocessCorrelationId(this.CorrelationId));
            log.Info("Process started: Instance ID={0}", id);
            this._subprocessInstanceId = id;
        }

        protected override void DoCancelTask()
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment", typeof(INGEnvironment));
            log.Info("Task[{0}]: Cancelling subprocess {0}", CorrelationId, _subprocessInstanceId);
            env.CancelProcessInstance(this._subprocessInstanceId);
            log.Debug("Subprocess {0} cancelled", _subprocessInstanceId);
        }

        protected override void DoExecuteTask()
        {
            throw new NotImplementedException();
        }

        public static string GetSubprocessCorrelationId(string taskCorrelationId)
        {
            return string.Format("_NGINN_{0}", taskCorrelationId);
        }

        public static string GetTaskCorrelationIdFromProcess(string processCorrelationId)
        {
            if (!IsSubprocessCorrelationId(processCorrelationId)) return null;
            return processCorrelationId.Substring(7);
        }

        public static bool IsSubprocessCorrelationId(string correlId)
        {
            return correlId.StartsWith("_NGINN_");
        }
    }
}

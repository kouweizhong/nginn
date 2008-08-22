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
    class SubprocessTaskActive : ActiveTaskBase
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private string _subprocessInstanceId;
        private string _subprocessDefinitionId;

        [NonSerialized]
        private SubprocessTask _task;

        public SubprocessTaskActive(SubprocessTask tsk)
            : base(tsk)
        {
            _subprocessInstanceId = tsk.SubprocessDefinitionId;
        }

        [TaskParameter(IsInput=true, Required=true, DynamicAllowed=true)]
        public string ProcessDefinitionId
        {
            get { return _subprocessDefinitionId; }
            set { _subprocessDefinitionId = value; }
        }

       
        public override void InitiateTask(IDataObject inputData)
        {
            INGEnvironment env = (INGEnvironment)Context.ParentProcess.Environment;

            IDictionary<string, object> inputVars = new Dictionary<string, object>();
            inputVars["_NGinn_ParentProcess"] = Context.ParentProcess.InstanceId;
            log.Info("Starting subprocess {0}", _task.SubprocessDefinitionId);
            IDataObject dob = inputData;
            string xml = dob.ToXmlString("data");
            string id = env.StartProcessInstance(_task.SubprocessDefinitionId, xml, GetSubprocessCorrelationId(this.CorrelationId));
            log.Info("Process started: Instance ID={0}", id);
            this._subprocessInstanceId = id;
        }

        protected override void DoInitiateTask()
        {
            throw new NotImplementedException();
        }


        public override void CancelTask()
        {
            INGEnvironment env = (INGEnvironment)Context.ParentProcess.Environment;
            log.Info("Task[{0}]: Cancelling subprocess {0}", CorrelationId, _subprocessInstanceId);
            env.CancelProcessInstance(this._subprocessInstanceId);
            log.Debug("Subprocess {0} cancelled", _subprocessInstanceId);
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

        public override void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            INGEnvironment env = (INGEnvironment)Context.ParentProcess.Environment;
            
            if (ite is SubprocessCompleted)
            {
                SubprocessCompleted sc = (SubprocessCompleted)ite;
                if (sc.SubprocessInstanceId != this._subprocessInstanceId)
                    throw new Exception();
                this.VariablesContainer = env.GetProcessOutputData(sc.SubprocessInstanceId);
                OnTaskCompleted();
            }
        }
    }

    /// <summary>
    /// Message to notify SubprocessTask that the subprocess has completed
    /// </summary>
    [Serializable]
    public class SubprocessCompleted : InternalTransitionEvent
    {
        public string SubprocessInstanceId;
    }
}

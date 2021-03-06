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
            
        }

        [TaskParameter(IsInput=true, Required=true, DynamicAllowed=true)]
        public string ProcessDefinitionId
        {
            get { return _subprocessDefinitionId; }
            set { _subprocessDefinitionId = value; }
        }

        

        protected override void DoInitiateTask()
        {
            INGEnvironment env = (INGEnvironment)Context.EnvironmentContext;

            IDictionary<string, object> inputVars = new Dictionary<string, object>();
            inputVars["_NGinn_ParentProcess"] = Context.ProcessInstanceId;
            log.Info("Starting subprocess {0}", ProcessDefinitionId);
            DataObject dob = VariablesContainer;
            string id = env.StartProcessInstance(ProcessDefinitionId, dob, null, GetSubprocessCorrelationId(this.CorrelationId));
            log.Info("Process started: Instance ID={0}", id);
            this._subprocessInstanceId = id;
        }


        public override void CancelTask()
        {
            INGEnvironment env = (INGEnvironment)Context.EnvironmentContext;
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

        public override bool HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            INGEnvironment env = (INGEnvironment)Context.EnvironmentContext;
            
            if (ite is SubprocessCompleted)
            {
                SubprocessCompleted sc = (SubprocessCompleted)ite;
                if (sc.SubprocessInstanceId != this._subprocessInstanceId)
                    throw new Exception();
                this.VariablesContainer = env.GetProcessOutputData(sc.SubprocessInstanceId);
                OnTaskCompleted();
                return true;
            }
            return false;
        }

        public override DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            dob["SubprocessDefinitionId"] = this.ProcessDefinitionId;
            dob["SubprocessInstanceId"] = this._subprocessInstanceId;
            return dob;
        }

        public override void RestoreState(DataObject dob)
        {
            base.RestoreState(dob);
            
            _subprocessDefinitionId = (string) dob["SubprocessDefinitionId"];
            _subprocessInstanceId = (string) dob["SubprocessInstanceId"];
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

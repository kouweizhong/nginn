using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NGinn.Engine;
using NLog;
using ScriptNET;
using System.Collections;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// Represents multi-instance transition. This class holds several
    /// instances of transitions
    /// </summary>
    [Serializable]
    public class MultiInstanceTaskActive : ActiveTransition, ITransitionCallback
    {
        private List<ActiveTransition> _transitions = new List<ActiveTransition>();
        
        public MultiInstanceTaskActive(Task tsk)
            : base(tsk)
        {
            log = LogManager.GetCurrentClassLogger();
        }

        public override void SetProcessInstance(ProcessInstance pi)
        {
            base.SetProcessInstance(pi);
            foreach (ActiveTransition at in _transitions)
            {
                at.SetProcessInstance(pi);
                at.ContainerCallback = this;
            }
        }

        public override void Activate()
        {
            base.Activate();
            foreach (ActiveTransition at in _transitions)
            {
                at.ContainerCallback = this;
                at.Activate();
            }
        }

        public override void Passivate()
        {
            foreach (ActiveTransition at in _transitions)
            {
                at.Passivate();
            }
            base.Passivate();
        }

        /// <summary>
        /// Return multi-instance task's sub-transition with given correlation Id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public ActiveTransition GetChildTransition(string correlationId)
        {
            foreach (ActiveTransition at in _transitions)
            {
                if (at.CorrelationId == correlationId) return at;
            }
            return null;
        }


        #region ITransitionCallback Members

        public void TransitionStarted(string correlationId)
        {
            lock (this)
            {
                ActiveTransition at = GetChildTransition(correlationId);
                if (at == null) throw new Exception("Child transition not found: " + correlationId);

                if (this.Status == TransitionStatus.ENABLED)
                {
                    if (!IsImmediate)
                    {
                        Status = TransitionStatus.STARTED;
                        _containerCallback.TransitionStarted(this.CorrelationId);
                    }
                }
            }
        }

        private void OnChildTransitionCompletedOrCancelled(string correlationId)
        {
            bool completed = true;
            foreach (ActiveTransition at in _transitions)
            {
                if (at.Status == TransitionStatus.COMPLETED ||
                    at.Status == TransitionStatus.CANCELLED)
                {
                }
                else
                {
                    completed = false;
                    break;
                }
            }
            if (completed)
            {
                log.Info("All multi-instance transitions have completed");
                if (!IsImmediate)
                {
                    Status = TransitionStatus.COMPLETED;
                    _containerCallback.TransitionCompleted(this.CorrelationId);
                }
            }
        }

        public void TransitionCompleted(string correlationId)
        {
            lock (this)
            {
                ActiveTransition theT = GetChildTransition(correlationId);
                if (theT == null) throw new Exception("Child transition not found: " + correlationId);

                ActiveTransitionCompleted compl = new ActiveTransitionCompleted();
                compl.CorrelationId = theT.CorrelationId;
                compl.InstanceId = this.ProcessInstanceId;
                compl.TaskId = theT.TaskId;
                compl.TaskType = ProcessTask.GetType().Name;
                compl.TimeStamp = DateTime.Now;
                compl.DefinitionId = _processInstance.ProcessDefinitionId;
                compl.MultiInstance = true;
                _processInstance.NotifyProcessEvent(compl);

                OnChildTransitionCompletedOrCancelled(correlationId);
            }
        }

        #endregion

        protected override void DoCancelTask()
        {
            lock (this)
            {
                foreach (ActiveTransition at in _transitions)
                {
                    at.CancelTask();
                }
            }
        }

        protected override void DoExecuteTask()
        {
            lock (this)
            {
                foreach (ActiveTransition at in _transitions)
                {
                    at.ExecuteTask();
                }
            }
        }

        protected override void DoInitiateTask()
        {
            lock (this)
            {
                foreach (ActiveTransition at in _transitions)
                {
                    at.InitiateTask();
                }
            }
        }

        public override void TransferInputDataToTask(IDataObject dataSource)
        {
            IScriptContext ctx = CreateTaskScriptContext(dataSource);
            log.Debug("Executing split query: {0}", ProcessTask.MultiInstanceSplitQuery);
            string q = ProcessTask.MultiInstanceSplitQuery.Trim();
            if (!q.EndsWith(";")) q += ";"; 
            object ret = Script.RunCode(q, ctx);
            log.Debug("Returned: {0}", ret);
            string alias = ProcessTask.MultiInstanceInputVariable;
            if (ret is ICollection)
            {
                IEnumerable enu = ret as IEnumerable;
                foreach (object ob in enu)
                {
                    ctx.SetItem(alias, ContextItem.Variable, ob);
                    InitiateMultiInstanceTask(ctx);
                }
            }
            else
            {
                ctx.SetItem(alias, ContextItem.Variable, ret);
                InitiateMultiInstanceTask(ctx);
            }
            log.Info("Multi-instance: created {0} tasks", _transitions.Count);
        }

        private void InitiateMultiInstanceTask(IScriptContext processDataCtx)
        {
            ActiveTransition at = null;// _processInstance.CreateSingleInstanceTransitionForTask(this.ProcessTask);
            at.ContainerCallback = this;
            log.Debug("Created new multi-instance task: {0}", at.CorrelationId);
            at.Activate();
            DataObject taskInput = new DataObject();
            DataBinding.ExecuteDataBinding(taskInput, ProcessTask.InputBindings, processDataCtx);
            this.SetTaskInputData(taskInput);
            _transitions.Add(at);
        }

        public override void ReceiveOutputDataFromTask(IDataObject dataTarget)
        {
            if (ProcessTask.MultiInstanceResultVariable == null ||
                ProcessTask.MultiInstanceResultVariable.Length == 0)
            {
                return;
            }
            ArrayList al = new ArrayList();
            IScriptContext ctx = CreateTaskScriptContext();
            foreach (ActiveTransition at in _transitions)
            {
                if (at.Status != TransitionStatus.COMPLETED)
                {
                    //al.Add(null);
                }
                else
                {
                    DataObject trg = new DataObject();
                    DataBinding.ExecuteDataBinding(trg, ProcessTask.OutputBindings, ctx);
                    al.Add(trg);
                }
            }
            dataTarget.Set(ProcessTask.MultiInstanceResultVariable, null, al);
        }

        /// <summary>
        /// Dispatch internal transition event to proper child transition
        /// </summary>
        /// <param name="ite"></param>
        public override void HandleInternalTransitionEvent(InternalTransitionEvent ite)
        {
            if (ite.CorrelationId == this.CorrelationId)
            {
                base.HandleInternalTransitionEvent(ite);
            }
            else
            {
                ActiveTransition at = GetChildTransition(ite.CorrelationId);
                if (at == null) throw new ApplicationException("Child transition not found: " + ite.CorrelationId);
                at.HandleInternalTransitionEvent(ite);
            }
        }
    }
}

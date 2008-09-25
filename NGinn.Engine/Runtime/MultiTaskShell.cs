using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using ScriptNET;
using System.Collections;


namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// Multi-instance task shell
    /// </summary>
    [Serializable]
    class MultiTaskShell : TaskShell, IActiveTaskContext
    { 
        public MultiTaskShell(ProcessInstance pi, Task tsk)
        {
            this.TaskId = tsk.Id;
            this.ProcessInstanceId = pi.InstanceId;
            SetProcessInstance(pi);
        }

        [Serializable]
        protected class TaskInfo
        {
            public IActiveTask ActiveTask;
            public TransitionStatus Status = TransitionStatus.ENABLED;
            public IDataObject OutputData;
            public IDataObject InputData;
        }

        private List<TaskInfo> _activeTasks = new List<TaskInfo>();

        public bool HasSubTask(string correlationId)
        {
            TaskInfo at = GetActiveTask(correlationId);
            return at != null;
        }

        protected TaskInfo GetActiveTask(string correlationId)
        {
            foreach (TaskInfo at in _activeTasks)
                if (at.ActiveTask.CorrelationId == correlationId) return at;
            return null;
        }

        

        public override void Activate()
        {
            base.Activate();
            foreach (TaskInfo at in _activeTasks)
            {
                at.ActiveTask.SetContext(this);
                at.ActiveTask.Activate();
            }
            _activated = true;
        }

        public override void Passivate()
        {
            base.Passivate();
            foreach (TaskInfo at in _activeTasks)
            {
                at.ActiveTask.Passivate();
            }
        }

        

        public override void InitiateTask(NGinn.Lib.Data.IDataObject sourceData)
        {
            IScriptContext ctx = this.CreateTaskScriptContext(sourceData);
            log.Debug("Executing split query: {0}", TaskDefinition.MultiInstanceSplitQuery);
            string q = TaskDefinition.MultiInstanceSplitQuery.Trim();
            if (!q.EndsWith(";")) q += ";";
            object ret = Script.RunCode(q, ctx);
            log.Debug("Returned: {0}", ret);
            string alias = TaskDefinition.MultiInstanceInputVariable;
            if (!(ret is IEnumerable))
            {
                ArrayList al = new ArrayList();
                al.Add(ret);
                ret = al;
            }
            IEnumerable enu = ret as IEnumerable;
            foreach (object ob in enu)
            {
                ctx.SetItem(alias, ContextItem.Variable, ob);
                DataObject taskInput = new DataObject();
                DataBinding.ExecuteDataBinding(taskInput, TaskDefinition.InputBindings, ctx);
                TaskInfo ti = new TaskInfo();
                ti.ActiveTask = CreateActiveTask();
                ti.ActiveTask.CorrelationId = ParentProcess.GetNextTransitionId();
                ti.ActiveTask.Activate();
                ti.InputData = taskInput;
                _activeTasks.Add(ti);
            }
            log.Info("Created {0} multi-instance tasks", _activeTasks.Count);
            foreach (TaskInfo ti in _activeTasks)
            {
                ti.ActiveTask.InitiateTask(ti.InputData);
            }
        }


        void IActiveTaskContext.TransitionStarted(string correlationId)
        {
            throw new NotImplementedException();
        }

        void IActiveTaskContext.TransitionCompleted(string correlationId, IDataObject taskOutputData)
        {
            bool found = false;
            bool completed = true;
            foreach(TaskInfo ti in _activeTasks)
            {
                if (ti.ActiveTask.CorrelationId == correlationId)
                {
                    ti.Status = TransitionStatus.COMPLETED;
                    ti.OutputData = taskOutputData;
                    found = true;
                }

                if (ti.Status != TransitionStatus.COMPLETED &&
                    ti.Status != TransitionStatus.CANCELLED)
                {
                    completed = false;
                }
            }
            if (!found) throw new Exception("Invalid correlation Id");
            if (completed)
            {
                this.Status = TransitionStatus.COMPLETED;
                this._parentCallback.TransitionCompleted(this.CorrelationId);
            }
        }

        public override void TransferTaskOutputDataToParent(IDataObject target)
        {
            if (TaskDefinition.MultiInstanceResultVariable == null ||
                TaskDefinition.MultiInstanceResultVariable.Length == 0)
            {
                return;
            }
            ArrayList al = new ArrayList();
            foreach (TaskInfo ti in _activeTasks)
            {
                if (ti.Status != TransitionStatus.COMPLETED) continue;
                IScriptContext ctx = CreateTaskScriptContext(ti.OutputData);
                DataObject trg = new DataObject();
                DataBinding.ExecuteDataBinding(trg, TaskDefinition.OutputBindings, ctx);
                al.Add(trg);
            }

            target.Set(TaskDefinition.MultiInstanceResultVariable, null, al);
        }



        #region IActiveTaskContext Members

        string IActiveTaskContext.CorrelationId
        {
            get { throw new NotImplementedException(); }
        }

        Task IActiveTaskContext.TaskDefinition
        {
            get { throw new NotImplementedException(); }
        }

        string IActiveTaskContext.ProcessInstanceId
        {
            get { throw new NotImplementedException(); }
        }

        TransitionStatus IActiveTaskContext.Status
        {
            get { throw new NotImplementedException(); }
        }

        INGEnvironmentContext IActiveTaskContext.Environment
        {
            get { throw new NotImplementedException(); }
        }

        string IActiveTaskContext.SharedId
        {
            get { throw new NotImplementedException(); }
        }

        Logger IActiveTaskContext.Log
        {
            get { return log; }
        }

        #endregion
    }
}

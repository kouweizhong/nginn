using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using System.Collections;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime
{

    /// <summary>
    /// Multi-instance task shell
    /// </summary>
    [Serializable]
    class MultiTaskShell : TaskShell, IActiveTaskContext
    { 
#warning "Multi task shell should contain TaskShells"

        public MultiTaskShell(ProcessInstance pi, string taskId)
        {
            this.TaskId = taskId;
            SetProcessInstance(pi);
        }

        public MultiTaskShell() : base()
        {
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
            ITaskScript ctx = this.CreateTaskScriptContext(sourceData);
            log.Debug("Executing split query: {0}", TaskDefinition.MultiInstanceSplitQuery);
            string alias = TaskDefinition.MultiInstanceInputVariable;
            object ret = ctx.EvalMultiInstanceSplitQuery();
            if (!(ret is IEnumerable))
            {
                ArrayList al = new ArrayList();
                al.Add(ret);
                ret = al;
            }
            IEnumerable enu = ret as IEnumerable;
            foreach (object ob in enu)
            {
                ctx.SourceData.Set(alias, null, ob);
                DataObject taskInput = new DataObject();
                DataBinding.ExecuteTaskInputDataBinding(taskInput, TaskDefinition.InputBindings, ctx);
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
                ITaskScript ctx = CreateTaskScriptContext(ti.OutputData);
                DataObject trg = new DataObject();
                DataBinding.ExecuteTaskOutputDataBinding(trg, TaskDefinition.OutputBindings, ctx);
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

        

        string IActiveTaskContext.SharedId
        {
            get { throw new NotImplementedException(); }
        }

        Logger IActiveTaskContext.Log
        {
            get { return log; }
        }

        #endregion

        public override DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            List<DataObject> ls = new List<DataObject>();
            foreach (TaskInfo ti in this._activeTasks)
            {
                DataObject dob2 = new DataObject();
                dob2["Status"] = ti.Status.ToString();
                if (ti.OutputData != null) dob2["OutputData"] = ti.OutputData;
                if (ti.ActiveTask != null) dob2["Subtask"] = SaveTaskState(ti.ActiveTask);
                ls.Add(dob2);
            }
            dob["Task"] = ls;
            return dob;
        }

        public override void RestoreState(DataObject state)
        {
            base.RestoreState(state);

        }
    }
}

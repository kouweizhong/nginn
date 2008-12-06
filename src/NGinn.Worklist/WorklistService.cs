using System;
using System.Collections.Generic;
using System.Text;
using Sooda;
using NGinn.Engine.Services;
using NLog;
using NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.Worklist;

namespace NGinn.Worklist
{
    public class WorklistService : IWorkListService
    {
       
        private SoodaTransaction StartTransaction()
        {
            return new SoodaTransaction(typeof(Task).Assembly);
        }
        
#region IWorkListService Members

        public void CreateWorkItem(WorkItem wi)
        {
            using (SoodaTransaction st = StartTransaction())
            {
                Task tsk = new Task(st);
                if (wi.AssigneeGroupId != null) tsk.AssigneeGroup = Group.GetRef(st, Convert.ToInt32(wi.AssigneeGroupId));
                if (wi.AssigneeId != null) tsk.Assignee = User.GetRef(st, Convert.ToInt32(wi.AssigneeId));
                tsk.CorrelationId = wi.CorrelationId;
                tsk.ProcessInstance = wi.ProcessInstanceId;
                tsk.Title = wi.Title;
                tsk.Description = wi.Description;
                tsk.TaskId = wi.TaskId;
                tsk.Status = TaskStatus.AssignedGroup;
                if (!wi.IsGroupResponsible)
                    tsk.Status = TaskStatus.Assigned;
                tsk.CreatedDate = DateTime.Now;
                if (wi.AssigneeGroupId != null) tsk.AssigneeGroup = (Group) st.GetObject(typeof(Group), wi.AssigneeGroupId);
                if (wi.AssigneeId != null) tsk.Assignee = (User)st.GetObject(typeof(User), wi.AssigneeId);
                
                st.Commit();
            }
        }


        private Task FindByCorrelationId(string instanceId, string correlationId, SoodaTransaction st)
        {
            TaskList tl = Task.GetList(st, TaskField.CorrelationId == correlationId);
            if (tl.Count > 0) return tl[0];
            return null;
        }

        public void CancelWorkItem(string correlationId)
        {
            using (SoodaTransaction st = StartTransaction())
            {
                Task tsk = FindByCorrelationId(null, correlationId, st);
                if (tsk == null) throw new Exception("Task not found");
                tsk.Status = TaskStatus.Cancelled;
                st.Commit();
            }
        }

        #endregion

        #region IWorkListService Members


        public void WorkItemCompleted(string correlationId)
        {
            using (SoodaTransaction st = StartTransaction())
            {
                Task tsk = FindByCorrelationId(null, correlationId, st);
                if (tsk == null) throw new ApplicationException("Task not found");
                tsk.Status = TaskStatus.Completed;
                tsk.ExecutionEnd = DateTime.Now;
                st.Commit();
            }

        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Sooda;
using NGinn.Engine.Services;
using NLog;
using NGinn.Worklist.BusinessObjects;

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
                tsk.TaskId = wi.TaskId;
                tsk.CreatedDate = DateTime.Now;
                st.Commit();
            }
        }

        


        public void CancelWorkItem(string correlationId)
        {
            
        }

        #endregion
    }
}

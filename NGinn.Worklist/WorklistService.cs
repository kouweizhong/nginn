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

        public string CreateWorkItem(WorkItem wi)
        {
            using (SoodaTransaction st = StartTransaction())
            {
                Task tsk = new Task();
                if (wi.AssigneeGroupId != null) tsk.AssigneeGroup = Group.GetRef(Convert.ToInt32(wi.AssigneeGroupId));
                if (wi.AssigneeId != null) tsk.Assignee = User.GetRef(Convert.ToInt32(wi.AssigneeId));
                tsk.ProcessInstance = wi.ProcessInstanceId;
                tsk.Title = wi.Title;
                st.Commit();
                return tsk.Id.ToString();
            }
        }
    }
}

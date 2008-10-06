using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using NGinn.Lib.Data;
using NGinn.Lib.Interfaces;
using Spring.Context;
using NGinn.Engine.Services;
using NG = NGinn.Lib.Schema;
using NLog;
using System.Collections.Specialized;
using Sooda;
using NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;

namespace NGinn.XmlFormsWWW
{
    /// <summary>
    /// Example of how task execution start could be handled in an application.
    /// We mark the task as 'processing' and notify NGinn that the task has been started.
    /// </summary>
    public partial class SelectTask : System.Web.UI.Page
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected void Page_Load(object sender, EventArgs e)
        {
            string corrId = Request["correlationId"];
            if (corrId == null) throw new ArgumentException("Missing correlationId", "correlationId");
            log.Debug("Method: {0}, Content: {1}", Request.HttpMethod, Request.ContentType);
            HandleSelection(corrId);
        }


        /// <summary>
        /// Hande task selection
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="taskOutputData"></param>
        private void HandleSelection(string correlationId)
        {
            try
            {
                IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
                INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment");
                IProcessDefinitionRepository pdr = (IProcessDefinitionRepository)ctx.GetObject("ProcessDefinitionRepository");
                int taskId;
                using (SoodaTransaction st = new SoodaTransaction(typeof(Task).Assembly))
                {
                    TaskList tl = Task.GetList(TaskField.CorrelationId == correlationId, SoodaSnapshotOptions.NoWriteObjects);
                    if (tl.Count == 0) throw new Exception("Task not found");
                    Task tsk = tl[0];
                    taskId = tsk.Id;
                    if (tsk.Status != TaskStatus.Assigned && tsk.Status != TaskStatus.AssignedGroup)
                        throw new Exception("Invalid task status");
                    tsk.Status = TaskStatus.Processing;
                    tsk.ExecutionStart = DateTime.Now;
                    TaskInstanceInfo ti = env.GetTaskInstanceInfo(correlationId);
                    env.NotifyTaskExecutionStarted(correlationId, Context.User.Identity.Name);
                    st.Commit();
                }
                Response.Redirect("TaskXml.aspx?id=" + taskId);
            }
            catch (Exception ex)
            {
                log.Error("Error completing task {0}: {1}", correlationId, ex);
                throw;
            }
        }
    }
}

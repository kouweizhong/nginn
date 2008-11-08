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

namespace NGinn.XmlFormsWWW
{
    public partial class CompleteTask : System.Web.UI.Page
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected void Page_Load(object sender, EventArgs e)
        {
            string corrId = Request["correlationId"];
            if (corrId == null) throw new ArgumentException("Missing correlationId", "correlationId");
            log.Debug("Method: {0}, Content: {1}", Request.HttpMethod, Request.ContentType);

            if (Request.HttpMethod == "POST")
            {
                if (Request.ContentType == "text/json")
                {
                }
                else if (Request.ContentType == "text/xml")
                {
                }
                else
                {
                    HandleCompletion(corrId, Request.Form);
                }
            }
            else if (Request.HttpMethod == "GET")
            {
                HandleCompletion(corrId, Request.QueryString);
            }
            else throw new Exception("Method not supported");
        }


        /// <summary>
        /// Hande task completion with task output data passed as NameValueCollection
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="taskOutputData"></param>
        private void HandleCompletion(string correlationId, NameValueCollection taskOutputData)
        {
            try
            {
                IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
                INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment");
                IProcessPackageRepository pdr = (IProcessPackageRepository)ctx.GetObject("PackageRepository");
                TaskInstanceInfo ti = env.GetTaskInstanceInfo(correlationId);
                NG.ProcessDefinition pd = pdr.GetProcess(ti.ProcessDefinitionId);
                NG.Task ntsk = pd.GetTask(ti.TaskId);
                StructDef sd = ntsk.GetTaskOutputDataSchema();
                DataObject completionData = new DataObject();
                foreach (MemberDef md in sd.Members)
                {
                    string s = taskOutputData[md.Name];
                    if (s != null)
                    {
                        completionData[md.Name] = s;
                    }
                }
                log.Info("Completing task {0} with data {1}", correlationId, completionData.ToString());
                env.ReportTaskFinished(correlationId, completionData, Context.User.Identity.Name);
            }
            catch (Exception ex)
            {
                log.Error("Error completing task {0}: {1}", correlationId, ex);
                throw;
            }
        }
    }
}

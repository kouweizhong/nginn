using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using NLog;
using System.IO;
using Spring.Context;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;

namespace NGinn.XmlFormsWWW
{
    public partial class StartProcess : System.Web.UI.Page
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod.Equals("POST"))
            {
                HandlePost();
            }
            else
            {
                ReportError("Post XML here to start a process");
                return;
            }
        }

        protected void HandlePost()
        {
            string definitionId = Request["definitionId"];
            if (definitionId == null)
            {
                ReportError("Missing definitionId");
                return;
            }
            log.Info("Definition id: {0}", definitionId);
            string xml = null;
            using (StreamReader sr = new StreamReader(Request.InputStream, Request.ContentEncoding))
            {
                xml = sr.ReadToEnd();
            }
            log.Info("Input XML: {0}", xml);
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            INGEnvironment env = (INGEnvironment) ctx.GetObject("NGEnvironment");
            string instanceId = env.StartProcessInstance(definitionId, xml);
            log.Info("Started process instance: {0}", instanceId);
            Response.Output.Write(instanceId);
            Response.End();
        }

        private void ReportError(string msg)
        {
            Response.StatusCode = 500;
            Response.Write(msg);
        }
    }
}

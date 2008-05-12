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
using NLog;
using NGinn.Lib.Interfaces;
using Spring.Context;

namespace NGinn.XmlFormsWWW.sys
{
    public partial class GetInstanceXml : System.Web.UI.Page
    {
        protected IApplicationContext _ctx;
        protected INGEnvironment _env;
            
        protected void Page_Load(object sender, EventArgs e)
        {
            _ctx = Spring.Context.Support.ContextRegistry.GetContext();
            _env = (INGEnvironment) _ctx.GetObject("NGEnvironment");
            string instId = Request["instanceId"];

            if (instId != null)
            {
                HandleGetInstanceData(instId);
                return;
            }
            string corrId = Request["correlationId"];
            if (corrId != null)
            {
                HandleGetTaskInstanceData(corrId);
                return;
            }
            throw new ApplicationException("Missing instanceId or correlationId");
        }

        protected void HandleGetInstanceData(string instanceId)
        {
            string s = _env.GetProcessInstanceData(instanceId);
            Response.ContentType = "text/xml";
            Response.Write(s);
            Response.End();
        }

        protected void HandleGetTaskInstanceData(string correlationId)
        {
            string s = _env.GetTaskInstanceXml(correlationId);
            Response.ContentType = "text/xml";
            Response.Write(s);
            Response.End();

        }
    }
}

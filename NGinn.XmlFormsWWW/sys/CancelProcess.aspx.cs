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
using NGinn.Lib.Interfaces;

namespace NGinn.XmlFormsWWW
{
    /// <summary>
    /// REST interface for cancelling process instances.
    /// </summary>
    public partial class CancelProcess : System.Web.UI.Page
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected void Page_Load(object sender, EventArgs e)
        {
            string instid = Request["instanceId"];
            if (instid == null) throw new Exception("missing instanceId");
            HandleCancelProcess(instid);
        }

        protected void HandleCancelProcess(string instanceId)
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            INGEnvironment env = (INGEnvironment) ctx.GetObject("NGEnvironment");
            env.CancelProcessInstance(instanceId);
        }
    }
}

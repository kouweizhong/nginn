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
using Spring.Context;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;


namespace NGinn.XmlFormsWWW
{
    public partial class GetSchema : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            IProcessDefinitionRepository pdr = (IProcessDefinitionRepository)ctx.GetObject("ProcessDefinitionRepository");
            string definitionId = Request["definitionId"];
            if (definitionId == null) throw new ApplicationException("missing definitionId parameter");
            Response.Output.Write(pdr.GetProcessInputSchema(definitionId));
            Response.End();
        }
    }
}

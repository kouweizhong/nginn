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
using Spring.Core;
using Spring.Context;
using NGinn.Lib.Interfaces.Worklist;

namespace NGinn.XmlFormsWWW
{
    public partial class ListData : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            ITODOListDataProvider prov = (ITODOListDataProvider)ctx.GetObject("TODOListDataProvider", typeof(ITODOListDataProvider));
            string xml = prov.GetListDataXml(null);
            Response.ContentType = "text/xml";
            Response.Output.Write(xml);
            Response.End();
        }
    }
}

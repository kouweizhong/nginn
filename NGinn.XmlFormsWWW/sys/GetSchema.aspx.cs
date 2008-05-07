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
            
            string pth = Request.PathInfo;
            if (pth.StartsWith("/")) pth = pth.Substring(1);
            string[] dt = pth.Split('/');
            if (dt.Length < 2) throw new Exception("Expected /<process definition id or package name>/<schema name>");
            string pkgid = dt[0];
            string schema = dt[1];
            string schemaXml = pdr.GetPackageSchema(pkgid, schema);
            Response.Output.Write(schemaXml);
            Response.End();
        }
    }
}

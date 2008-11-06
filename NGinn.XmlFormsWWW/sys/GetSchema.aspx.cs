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
    /// <summary>
    /// virtual "directory" structure
    /// GetSchema.aspx/Package.Process/input - process input schema
    /// GetSchema.aspx/Package.Process/output - process output schema
    /// GetSchema.aspx/Package.Process/task/Task_Id/input - task input schema
    /// GetSchema.aspx/Package.Process/task/Task_Id/output - task output schema
    /// </summary>
    public partial class GetSchema : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            IProcessDefinitionRepository pdr = (IProcessDefinitionRepository)ctx.GetObject("ProcessDefinitionRepository");
            
            string pth = Request.PathInfo;
            if (pth.StartsWith("/")) pth = pth.Substring(1);
            string[] dt = pth.Split('/');
            if (dt.Length < 2) 
            {
                return;
                //throw new Exception("Expected /<process definition id or package name>/<schema name>");
            }
            string pkgid = dt[0];
            string schema = dt[1];
            string xml = null;
            /*
            if (string.Compare(schema, "input", true) == 0)
            {
                xml = pdr.GetProcessInputSchema(pkgid);
            }
            else if (String.Compare(schema, "output", true) == 0)
            {
                xml = pdr.GetProcessOutputSchema(pkgid);
            }
            else if (string.Compare(schema, "internal", true) == 0)
            {
                xml = pdr.GetProcessInternalDataSchema(pkgid);
            }
            else if (string.Compare(schema, "task", true) == 0)
            {
                if (dt.Length < 4) throw new Exception("Expected task ID");
                string taskId = dt[2];
                string sch2 = dt[3];
                if (String.Compare(sch2, "input", true) == 0)
                {
                    xml = pdr.GetTaskInputSchema(pkgid, taskId);
                }
                else if (String.Compare(sch2, "output", true) == 0)
                {
                    xml = pdr.GetTaskOutputSchema(pkgid, taskId);
                }
                else if (String.Compare(sch2, "internal", true) == 0)
                {
                    xml = pdr.GetTaskInternalDataSchema(pkgid, taskId);
                }
                else throw new Exception("Expected /task/<taskid>/[input|output|internal]");
            }
            else throw new Exception("Expected /<definition Id>/[input|output|internal|task]");
            */
            Response.ContentType = "text/xml";
            Response.Output.Write(xml);
            Response.End();
        }
    }
}

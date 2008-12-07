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
using NGinn.Lib.Data;

namespace NGinn.XmlFormsWWW
{
    public partial class DispatchProcessMessage : System.Web.UI.Page
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
                ReportError("usage: DispatchProcessMessage.aspx?messageCorrelationId=[message correlation ID here]. Use POST with message body inside.");
                return;
            }
        }

        protected void HandlePost()
        {
            string xml = null;
            using (StreamReader sr = new StreamReader(Request.InputStream, Request.ContentEncoding))
            {
                xml = sr.ReadToEnd();
            }
            log.Info("Input XML: {0}", xml);
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            DataObject dob = xml.Length == 0 ? new DataObject() : DataObject.ParseXml(xml);
            
            INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment");
            string msgId = Request["messageCorrelationId"];
            if (msgId == null)
            {
                string msgExpr = Request["correlationIdField"];
                if (msgExpr == null)
                {
                    ReportError("Missing messageCorrelationId or correlationIdField");
                    Response.End();
                }
                msgId = (string) dob.GetValue(msgExpr);
                if (msgId == null || msgId.Length == 0)
                {
                    ReportError("correlationIdField returned no data");
                    Response.End();
                }
            }

            env.DispatchProcessMessage(msgId, dob);
            Response.End();
        }

        private void ReportError(string msg)
        {
            Response.StatusCode = 500;
            Response.Write(msg);
        }
    }
}

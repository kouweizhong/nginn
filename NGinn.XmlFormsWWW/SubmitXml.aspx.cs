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

namespace NGinn.XmlFormsWWW
{
    public partial class SubmitXml : System.Web.UI.Page
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod == "POST")
            {
                
            }
            else
            {

            }
        }
    }
}

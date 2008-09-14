using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Spring.Context;

namespace NGinn.XmlFormsWWW
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}
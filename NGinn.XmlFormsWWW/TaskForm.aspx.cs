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
using Sooda;
using WK = NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;
using NGinn.Lib.Schema;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;

namespace NGinn.XmlFormsWWW
{
    public partial class TaskForm : SoodaPage
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private WK.Task _theTask;

        public WK.Task TheTask
        {
            get { return _theTask; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            log.Info("Page_Load");
            int id = Int32.Parse(Request["id"]);
            _theTask = WK.Task.GetRef(id);
        }
    }
}

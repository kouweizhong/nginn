﻿using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Sooda;
using NGinn.Worklist;
using NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;
using Spring.Context;

namespace NGinn.XmlFormsWWW
{
    public partial class TODO : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            using (SoodaTransaction st = new SoodaTransaction(typeof(Task).Assembly))
            {
                TaskList tl = Task.GetList(TaskField.Status.In(TaskStatus.Assigned, TaskStatus.AssignedGroup, TaskStatus.Processing), SoodaSnapshotOptions.NoWriteObjects);
                this.GridView1.DataSource = tl;
                this.GridView1.DataBind();
            }
        }
    }
}

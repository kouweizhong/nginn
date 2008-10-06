using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using NLog;
using Sooda;
using WK = NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;
using NG = NGinn.Lib.Schema;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;
using System.Xml;
using Spring.Context;
using NGinn.Engine.Services;

namespace NGinn.XmlFormsWWW
{
    public partial class TaskXml : SoodaPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "text/xml";
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            XmlWriter xw = XmlWriter.Create(Response.Output, xws);
            xw.WriteStartDocument();
            xw.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"task_details.xsl\"");
            xw.WriteStartElement("Task");

            WK.Task tsk = WK.Task.GetRef(Int32.Parse(Request["id"]));
            xw.WriteElementString("Id", tsk.Id.ToString());
            xw.WriteElementString("Assignee", tsk.Assignee == null ? "" : tsk.Assignee.Name);
            xw.WriteElementString("AssigneeGroup", tsk.AssigneeGroup == null ? "" : tsk.AssigneeGroup.Name);
            xw.WriteElementString("Status", tsk.Status.Id.ToString());
            xw.WriteElementString("StatusName", tsk.Status.Name);
            xw.WriteElementString("TaskId", tsk.TaskId.IsNull ? "" : tsk.TaskId.Value);
            xw.WriteElementString("Title", tsk.Title);
            xw.WriteElementString("Description", tsk.Description.IsNull ? "" : tsk.Description.Value);
            xw.WriteElementString("CreatedDate", tsk.CreatedDate.ToString());
            xw.WriteElementString("CorrelationId", tsk.CorrelationId.IsNull ? "" : tsk.CorrelationId.Value);
            xw.WriteElementString("ExecutionEnd", tsk.ExecutionEnd.IsNull ? "" : tsk.ExecutionEnd.Value.ToString());
            xw.WriteElementString("ExecutionStart", tsk.ExecutionStart.IsNull ? "" : tsk.ExecutionStart.Value.ToString());

            OutputNGinnTaskData(tsk, xw);

            xw.WriteEndElement();
            xw.Flush();
        }

        protected void OutputNGinnTaskData(WK.Task tsk, XmlWriter xw)
        {
            IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
            INGEnvironment env = (INGEnvironment)ctx.GetObject("NGEnvironment");
            DataObject dob = env.GetTaskData(tsk.CorrelationId.Value);
            IProcessDefinitionRepository pdr = (IProcessDefinitionRepository)ctx.GetObject("ProcessDefinitionRepository");
            TaskInstanceInfo ti = env.GetTaskInstanceInfo(tsk.CorrelationId.Value);
            NG.ProcessDefinition pd = pdr.GetProcessDefinition(ti.ProcessDefinitionId);
            NG.Task ntsk = pd.GetTask(ti.TaskId);

            xw.WriteStartElement("NGinnTaskData");
            foreach (VariableDef vd in ntsk.TaskVariables)
            {
                xw.WriteStartElement("field");
                xw.WriteAttributeString("name", vd.Name);
                if (dob[vd.Name] != null) xw.WriteAttributeString("value", dob[vd.Name].ToString());
                if (vd.VariableDir == VariableDef.Dir.Out ||
                        vd.VariableDir == VariableDef.Dir.InOut)
                {
                    xw.WriteAttributeString("access", vd.IsRequired ? "required" : "modify");
                }
                else
                {
                    xw.WriteAttributeString("access", "read");
                }
                xw.WriteAttributeString("type", vd.TypeName);
                if (pd.DataTypes.IsEnumType(vd.TypeName))
                {
                    xw.WriteAttributeString("field_type", "select");
                    EnumDef ed = (EnumDef) pd.DataTypes.GetTypeDef(vd.TypeName);
                    foreach(object v in ed.EnumValues)
                    {
                        xw.WriteElementString("option", Convert.ToString(v));
                    }
                }
                xw.WriteEndElement();
                
            }
            


            xw.WriteEndElement();
        }
    }
}

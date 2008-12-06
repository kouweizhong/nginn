using System;
using System.Collections.Generic;
using System.Text;
using Sooda;
using NLog;
using NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;
using System.Xml;
using NGinn.Lib.Interfaces.Worklist;

namespace NGinn.Worklist
{
    public class TaskListDataProvider : ITODOListDataProvider
    {
        public string GetListDataXml(string dataQuery)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            XmlWriter xw = XmlWriter.Create(sb, ws);
            xw.WriteStartElement("results");
            using (SoodaTransaction st = new SoodaTransaction(typeof(Task).Assembly))
            {
                TaskList tl = Task.GetList(TaskField.Status.In(TaskStatus.Assigned, TaskStatus.AssignedGroup, TaskStatus.Processing), SoodaSnapshotOptions.NoWriteObjects);
                foreach (Task tsk in tl)
                {
                    xw.WriteStartElement("row");
                    xw.WriteElementString("Id", tsk.Id.ToString());
                    xw.WriteElementString("Title", tsk.Title);
                    xw.WriteElementString("CreatedDate", tsk.CreatedDate.ToString());
                    xw.WriteElementString("CorrelationId", tsk.CorrelationId.ToString());
                    xw.WriteElementString("Assignee.Name", Convert.ToString(tsk.Evaluate("Assignee.Name")));
                    xw.WriteElementString("AssigneeGroup.Name", Convert.ToString(tsk.Evaluate("AssigneeGroup.Name")));
                    xw.WriteElementString("Status_Name", tsk.Status.Name);
                    xw.WriteElementString("TaskId", tsk.TaskId.ToString());
                    xw.WriteEndElement();
                }
            }
            xw.WriteEndElement();
            xw.Flush();
            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// XmlHttpTask makes a HTTP request in both synchronous and asynchronous manner.
    /// It sends XML data by HTTP post request and receives XML response.
    /// In async mode the task waits for external application to post xml response to nginn web server.
    /// </summary>
    [Serializable]
    public class XmlHttpTask : Task
    {

        public override TaskParameterInfo[] GetTaskParameters()
        {
            TaskParameterInfo[] tpis = new TaskParameterInfo[] {
                new TaskParameterInfo("Url", typeof(string), true, true, true),
                new TaskParameterInfo("RequestXslt", typeof(string), false, true, true),
                new TaskParameterInfo("ResponseXslt", typeof(string), false, true, true),
            };
            return tpis;
        }

        
    }
}

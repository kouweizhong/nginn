using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class ScriptTask : Task
    {
        private string _code;

        public string Script
        {
            get { return _code; }
            set { _code = value; }
        }

       

        public override TaskParameterInfo[] GetTaskParameters()
        {
            return new TaskParameterInfo[] {
                new TaskParameterInfo("ScriptBody", typeof(string), true, true, false)
            };
        }
    }
}

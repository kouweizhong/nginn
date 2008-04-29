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
        private Uri _targetUri;
        private bool _isAsync = false;
        private string _outputDataXslFile = null;
        private string _inputDataXslFile = null;
        private string _httpMethod = "POST";

        public Uri TargetUri
        {
            get { return _targetUri; }
            set { _targetUri = value; }
        }
        
        public bool IsAsync
        {
            get { return _isAsync; }
            set { _isAsync = value; }
        }
        
        public string OutputDataXsl
        {
            get { return _outputDataXslFile; }
            set { _outputDataXslFile = value; }
        }
        
        public string InputDataXsl
        {
            get { return _inputDataXslFile; }
            set { _inputDataXslFile = value; }
        }

        public string HttpMethod
        {
            get { return _httpMethod; }
            set { _httpMethod = value; }
        }

        public override bool IsImmediate
        {
            get { return !IsAsync; }
        }
    }
}

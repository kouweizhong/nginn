using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using System.Xml;
using System.IO;
using System.Xml.Xsl;
using System.Net;
using Mvp.Xml;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// XmlHttp task implementation based on the curl program.
    /// </summary>
#warning TODO
    [Serializable]
    public class XmlHttpTaskActiveCurl : ActiveTaskBase
    {

        public XmlHttpTaskActiveCurl(Task tsk)
            : base(tsk)
        {
        }


        private string _url;
        private string _responseText = null;
        
        /// <summary>
        /// Request URL
        /// </summary>
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }

        private string _userName;
        
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        private string _password;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public enum RequestMode
        {
            Get,
            Post,
            XmlPost,
            JsonPost
        }

        private RequestMode _rqMode;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public RequestMode RequestType
        {
            get { return _rqMode; }
            set { _rqMode = value; }
        }

        /// <summary>
        /// Http response text - output parameter
        /// If Response XSLT is specified, response text will contain 
        /// transformed XML.
        /// </summary>
        [TaskParameter(IsInput = false)]
        public string ResponseText
        {
            get { return _responseText; }
        }

        
        protected override void DoInitiateTask()
        {
            DataObject dob = this.VariablesContainer;

            string curlArgs = GetCurlCommandArguments();
            log.Info("Curl arguments: {0}", curlArgs);
        }

        /// <summary>
        /// cancellation not supported
        /// </summary>
        public override void CancelTask()
        {
            throw new NotImplementedException();
        }


        public override DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            dob["Url"] = _url;
            return dob;
        }

        public override void RestoreState(DataObject dob)
        {
            base.RestoreState(dob);
            if (!dob.TryGet("Url", ref _url)) throw new Exception("Missing Url");
        }

        private IHttpClient _httpClient;
        public IHttpClient HttpClient
        {
            get { return _httpClient; }
            set { _httpClient = value; }
        }

        private string _curlArguments;
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string CurlArguments
        {
            get { return _curlArguments; }
            set { _curlArguments = value; }
        }

        protected string GetCurlCommandArguments()
        {
            StringBuilder sb = new StringBuilder();
            if (CurlArguments != null) sb.Append(CurlArguments + " ");
            if (UserName != null && UserName.Length > 0)
            {
                sb.Append(string.Format(" -u {0}:{1}", UserName, Password));
            }
            if (this.RequestType == RequestMode.Post)
            {
                DataObject dob = VariablesContainer;
                foreach (VariableDef vd in Context.TaskDefinition.TaskVariables)
                {
                    if (vd.VariableDir == VariableDef.Dir.In ||
                        vd.VariableDir == VariableDef.Dir.InOut ||
                        vd.VariableDir == VariableDef.Dir.Local)
                    {
                        string namme = vd.Name;
                        object val = dob[namme];
                        if (val != null)
                        {
                            sb.Append(string.Format(" -F \"{0}={1}\"", namme, val));
                        }
                    }
                }
            }
            else throw new Exception("Request type not supported");
            sb.Append(Url);
            return sb.ToString();
        }
    }
}

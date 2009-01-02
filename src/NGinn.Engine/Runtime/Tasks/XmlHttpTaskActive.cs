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

namespace NGinn.Engine.Runtime.Tasks
{
    /// <summary>
    /// XmlHttp task implementation. Sends/receives HTTP requests.
    /// possible versions
    /// - send/receive XML in a POST request 
    /// - send/receive SOAP (web services) - using XSLT for SOAP message creation
    /// - send http GET requests, receive xml
    /// - send http POST requests - multipart/form data
    /// 1. send/receive XML
    /// task data is sent to a http url as XML POST. Before sending it can be xslt-transformed.
    /// after receiving, system assumess XML and can transform it using response XSLT.
    /// Transformed xml should be consistent with task's output variables schema, because
    /// it will provide values for output variables.
    /// The same applies to SOAP - only the xslt is different.
    /// 2. GET/POST with multipart/form data
    /// some work is needed here
    /// 3. Error handling.
    /// In case of errors (HTTP error status codes) - WHAT TODO HERE?
    /// 
    /// </summary>
#warning TODO
    [Serializable]
    public class XmlHttpTaskActive : ActiveTaskBase
    {

        public XmlHttpTaskActive(Task tsk)
            : base(tsk)
        {
        }

        public enum ResponseType
        {
            Text,
            Xml,
            Json,
            Auto
        }

        public enum RequestType
        {
            /// <summary>Send http get with input variables as parameters</summary>
            HttpGet,
            /// <summary>Send http post with input variables as form parameters</summary>
            HttpPost,
            /// <summary>Send http post with xml data in body</summary>
            XmlPost,
            /// <summary>Send http post with json data in body</summary>
            JsonPost
        }

        private string _url;
        private string _responseXslt;
        private string _requestXslt;
        private ResponseType _respType = ResponseType.Xml;
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
        private string _passwd;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string Password
        {
            get { return _passwd; }
            set { _passwd = value; }
        }
       
        /// <summary>
        /// Name of XSLT to be applied to request XML
        /// </summary>
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string RequestXslt
        {
            get { return _requestXslt; }
            set { _requestXslt = value; }
        }

        
        /// <summary>
        /// Name of XSLT to be applied to response XML
        /// </summary>
        [TaskParameter(IsInput=true, Required=false, DynamicAllowed=true)]
        public string ResponseXslt
        {
            get { return _responseXslt; }
            set { _responseXslt = value; }
        }

        private string _authMethod = null;

        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string AuthMethod
        {
            get { return _authMethod; }
            set { _authMethod = value; }
        }

        private RequestType _rqMode;
        [TaskParameter(IsInput = true, Required = true, DynamicAllowed = true)]
        public RequestType RequestMode
        {
            get { return _rqMode; }
            set { _rqMode = value; }
        }
        /// <summary>
        /// Response interpretation
        /// </summary>
        [TaskParameter(IsInput = true, Required = true, DynamicAllowed = true)]
        public ResponseType ResponseMode
        {
            get { return _respType; }
            set { _respType = value; }
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

        private int _httpStatus;
        [TaskParameter(IsInput = false)]
        public int ResponseStatus
        {
            get { return _httpStatus; }
        }

        private string _requesttEncoding = "utf-8";
        [TaskParameter(IsInput = true, Required=false, DynamicAllowed=true)]
        public string RequestEncoding
        {
            get { return _requesttEncoding; }
            set { _requesttEncoding = value; }
        }

        protected string TransformXml(string xmlStr, string xsltName)
        {
            if (xsltName == null || xsltName.Length == 0) return xmlStr;
            return xmlStr;
        }

        protected override void DoInitiateTask()
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            DataObject dob = this.VariablesContainer;
            if (ResponseXslt != null && ResponseXslt.Length > 0 && ResponseMode == ResponseType.Text) throw new Exception("XSLT cannot be specified when ResponseType is not XML");

            string xmlStr = dob.ToXmlString("Data");
            if (RequestXslt != null && RequestXslt.Length > 0)
            {
                xmlStr = TransformXml(xmlStr, RequestXslt);
            }
            try
            {
                string url = this.Url;
                log.Info("Sending HTTP request to {0}", url);

                RequestDelegate reqd;
                ResponseDelegate respd;
                if (RequestMode == RequestType.HttpGet)
                    reqd = PrepareGetRequest;
                else if (RequestMode == RequestType.HttpPost)
                    reqd = PreparePostRequest;
                else if (RequestMode == RequestType.XmlPost)
                    reqd = PrepareXmlPostRequest;
                else if (RequestMode == RequestType.JsonPost)
                    reqd = PrepareJsonPostRequest;
                else throw new Exception();

                if (ResponseMode == ResponseType.Auto)
                    respd = HandleAutoResponse;
                else if (ResponseMode == ResponseType.Json)
                    respd = HandleJsonResponse;
                else if (ResponseMode == ResponseType.Text)
                    respd = HandleTextResponse;
                else if (ResponseMode == ResponseType.Xml)
                    respd = HandleXmlResponse;
                else throw new Exception();

                DoRequest(Url, reqd, respd);

                this.OnTaskCompleted();
            }
            catch (Exception ex)
            {
                log.Error("Request error: {0}", ex);
                throw;
            }
        }

        /// <summary>
        /// cancellation not supported
        /// </summary>
        public override void CancelTask()
        {
            throw new NotImplementedException();
        }

        

        protected IDictionary<string, string> GetRequestData()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            foreach (VariableDef vd in Context.TaskDefinition.TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.In ||
                    vd.VariableDir == VariableDef.Dir.InOut ||
                    (vd.VariableDir == VariableDef.Dir.Local && vd.DefaultValueExpr != null))
                {
                    d[vd.Name] = VariablesContainer[vd.Name].ToString();
                }
            }
            return d;
        }


        protected delegate void RequestDelegate(HttpWebRequest wrq);
        protected delegate void ResponseDelegate(HttpWebResponse resp);

        protected void DoRequest(string url, RequestDelegate prepareRq, ResponseDelegate handleResp)
        {
            try
            {
                StringBuilder surl = new StringBuilder(Url);
                IDictionary<string, string> param = GetRequestData();
                if (RequestMode == RequestType.HttpGet)
                {
                    foreach (string k in param.Keys)
                    {
                        surl.Append(string.Format("&{0}={1}", k, param[k]));
                    }
                }
                log.Debug("Making request to {0}", surl.ToString());
                HttpWebRequest wrq = (HttpWebRequest)WebRequest.Create(surl.ToString());
                if (this.RequestMode == RequestType.HttpGet)
                    wrq.Method = "GET";
                else
                    wrq.Method = "POST";
                if (AuthMethod != null && AuthMethod.Length > 0)
                {
                    if ("Basic" == AuthMethod ||
                        "Digest" == AuthMethod)
                    {
                        //CredentialCache cc = new CredentialCache();
                        //cc.Add(new Uri(Url), AuthMethod, new NetworkCredential(UserName, Password));
                        //wrq.Credentials = cc;
                        wrq.Credentials = new NetworkCredential(UserName, Password);
                    }
                    else throw new Exception("Unsupported auth method: " + AuthMethod);
                }
                prepareRq(wrq);
                using (HttpWebResponse resp = (HttpWebResponse)wrq.GetResponse())
                {
                    _httpStatus = (int)resp.StatusCode;
                    log.Info("Got response: Status={0} ({1}), Content: {2}", (int)resp.StatusCode, resp.StatusDescription, resp.ContentType);
                    handleResp(resp);
                }
            }
            catch (Exception ex)
            {
                log.Error("Error making request to {0}: {1}", url, ex);
                throw new Exception(Url, ex);
            }
        }

        protected void PrepareGetRequest(HttpWebRequest wrq)
        {
        }

        protected void PreparePostRequest(HttpWebRequest wrq)
        {
            IDictionary<string, string> dic = GetRequestData();
            Encoding encoding = Encoding.GetEncoding(RequestEncoding);
            StringBuilder postData = new StringBuilder();
            foreach (string key in dic.Keys)
            {
                if (postData.Length > 0) postData.Append("&");
                postData.AppendFormat("{0}={1}", key, dic[key]);
            }
            log.Debug("Sending data: {0}", postData.ToString());
            byte[] data = encoding.GetBytes(postData.ToString());
            wrq.ContentType = "application/x-www-form-urlencoded";
            wrq.ContentLength = data.Length;
            using (Stream stm = wrq.GetRequestStream())
            {
                stm.Write(data, 0, data.Length);
            }
        }

        protected void PrepareXmlPostRequest(HttpWebRequest wrq)
        {
            wrq.ContentType = "text/xml";
            DataObject dob = VariablesContainer;
            using (Stream stm = wrq.GetRequestStream())
            {
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.Encoding = Encoding.UTF8;
                xws.Indent = true;
                XmlWriter xw = XmlWriter.Create(stm, xws);
                xw.WriteStartDocument();
                dob.ToXml("Data", xw);
                xw.Flush();
            }
        }

        protected void PrepareJsonPostRequest(HttpWebRequest wrq)
        {
            throw new NotImplementedException();
        }

        protected void HandleXmlResponse(HttpWebResponse resp)
        {
            string charset = resp.CharacterSet;
            if (charset == null || charset.Length == 0) charset = "utf-8";
            using (Stream stm = resp.GetResponseStream())
            {
                StreamReader sr = new StreamReader(stm, Encoding.GetEncoding(charset));
                string xml = sr.ReadToEnd();
                log.Debug("Received xml: {0}", xml);
                DataObject dob = DataObject.ParseXml(xml);
                this.UpdateTaskData(dob);
            }
        }

        protected void HandleJsonResponse(HttpWebResponse resp)
        {
            throw new NotImplementedException();
        }

        protected void HandleTextResponse(HttpWebResponse resp)
        {
            using (Stream stm = resp.GetResponseStream())
            {
                StreamReader sr = new StreamReader(stm, Encoding.GetEncoding(resp.ContentEncoding));
                this._responseText = sr.ReadToEnd();
            }
        }

        protected void HandleAutoResponse(HttpWebResponse resp)
        {
            if (resp.ContentType == "text/xml" || resp.ContentType == "application/xml")
            {
                HandleXmlResponse(resp);
                return;
            }
            else if (resp.ContentType == "text/json" || resp.ContentType == "application/json")
            {
                HandleJsonResponse(resp);
                return;
            }
            else
            {
                HandleTextResponse(resp);
                return;
            }
        }

        private string _xmlRoot = "data";
        [TaskParameter(IsInput=true, Required=false)]
        public string XmlRoot
        {
            get { return _xmlRoot; }
            set { _xmlRoot = value; }
        }

        public override DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            dob["Url"] = _url;
            dob["RequestXslt"] = _requestXslt;
            dob["ResponseXslt"] = _responseXslt;
            dob["ResponseStatus"] = _httpStatus;
            dob["RequestMode"] = RequestMode.ToString();
            dob["ResponseMode"] = ResponseMode.ToString();
            dob["AuthMethod"] = AuthMethod;
            dob["UserName"] = UserName;
            return dob;
            
        }

        public override void RestoreState(DataObject dob)
        {
            base.RestoreState(dob);
            if (!dob.TryGet("Url", ref _url)) throw new Exception("Missing Url");
            dob.TryGet("RequestXslt", ref _requestXslt);
            dob.TryGet("ResponseXslt", ref _responseXslt);
            dob.TryGet("ResponseStatus", ref _httpStatus);
            string s = null;
            dob.TryGet("RequestMode", ref s);
            this._rqMode = (RequestType) Enum.Parse(typeof(RequestType), s);
            dob.TryGet("ResponseMode", ref s);
            this._respType = (ResponseType)Enum.Parse(typeof(ResponseType), s);
            dob.TryGet("AuthMethod", ref _authMethod);
            dob.TryGet("UserName", ref _userName);
        }
    }
}

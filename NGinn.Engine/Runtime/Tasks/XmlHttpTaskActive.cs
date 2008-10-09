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
            Xml
        }


        private string _url;
        private string _responseXslt;
        private string _requestXslt;
        private string _method = "POST";
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


        /// <summary>
        /// Http method to use
        /// </summary>
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public string Method
        {
            get { return _method; }
            set { _method = value; }
        }

        /// <summary>
        /// Response interpretation
        /// </summary>
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
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

        protected string TransformXml(string xmlStr, string xsltName)
        {
            if (xsltName == null || xsltName.Length == 0) return xmlStr;
            return xmlStr;
        }

        protected override void DoInitiateTask()
        {
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

                WebClient wc = new WebClient();
                string resp = wc.UploadString(url, Method, xmlStr);

                _responseText = resp;
                if (ResponseMode == ResponseType.Xml)
                {
                }
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


        public override DataObject SaveState()
        {
            DataObject dob = base.SaveState();
            dob["Url"] = _url;
            dob["RequestXslt"] = _requestXslt;
            dob["ResponseXslt"] = _responseXslt;
            dob["Method"] = Method;
            return dob;
        }

        public override void RestoreState(DataObject dob)
        {
            base.RestoreState(dob);
            if (!dob.TryGet("Url", ref _url)) throw new Exception("Missing Url");
            dob.TryGet("RequestXslt", ref _requestXslt);
            dob.TryGet("ResponseXslt", ref _responseXslt);
            dob.TryGet("Method", ref _method);
        }
    }
}

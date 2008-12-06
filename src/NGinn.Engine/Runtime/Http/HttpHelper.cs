using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Web;

namespace NGinn.Engine.Runtime.Http
{
    /// <summary>
    /// Helper class for making HTTP requests and receiving response
    /// </summary>
    public class HttpHelper
    {
        public delegate void SuccessHandler();
        public delegate void ErrorHandler();



        public void Get(string url, IDictionary<string, string> parameters, string expectedDataType, SuccessHandler onSuccess, ErrorHandler onError)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string k in parameters.Keys)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(HttpUtility.UrlEncode(k + "=" + parameters[k]));
            }
            string u2;
            if (url.IndexOf('?') > 0)
                u2 = url + "&" + sb.ToString();
            else
                u2 = url + "?" + sb.ToString();

            HttpWebRequest wrq = (HttpWebRequest) HttpWebRequest.Create(u2);
            HttpWebResponse resp = (HttpWebResponse) wrq.GetResponse();
            
            WebClient wc = new WebClient();
            
        }

        public void Post(string url, IDictionary<string, string> parameters, string expectedDataType, SuccessHandler onSuccess, ErrorHandler onError)
        {

        }

        
    }
}

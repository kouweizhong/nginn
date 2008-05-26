using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Collections.Specialized;

namespace XmlProc
{
    public interface IElementHandlerFactory
    {
        IElementHandler GetHandlerFor(string elementName, string namespaceUri);
        bool HandlesNamespace(string namespaceUri);
    }

    public class FormProcessorFactory : IElementHandlerFactory
    {
        private Dictionary<string, Dictionary<string, Type>> _handlerMap = new Dictionary<string, Dictionary<string, Type>>();
        private string _formBaseDir; //= Atmo.AtmoConfig.GetString("XmlProc.FormProcessorFactory.BaseDir");
        private static FormProcessorFactory _defaultInstance = new FormProcessorFactory();

        public FormProcessorFactory()
        {
            RegisterHandlersFromAssembly(typeof(FormProcessorFactory).Assembly);
        }

        public FormProcessorFactory(string formDir)
        {
            _formBaseDir = formDir;
            RegisterHandlersFromAssembly(typeof(FormProcessorFactory).Assembly);
        }


        public void RegisterHandlersFromAssembly(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                RegisterHandler(t);
            }
        }

        public void RegisterHandler(Type t)
        {
            foreach (ElementHandlerAttribute attr in t.GetCustomAttributes(typeof(ElementHandlerAttribute), false))
            {
                RegisterHandler(t, attr.ElementName, attr.NamespaceUri);
            }
        }

        public void RegisterHandler(Type t, string elementName, string namespaceUri)
        {
            if (elementName == null) elementName = t.Name.ToLower();
            lock (_handlerMap)
            {
                Dictionary<string, Type> d;
                if (_handlerMap.ContainsKey(namespaceUri))
                    d = _handlerMap[namespaceUri];
                else
                {
                    d = new Dictionary<string, Type>();
                    _handlerMap[namespaceUri] = d;
                }
                if (!d.ContainsKey(elementName)) d[elementName] = t;
            }
        }

        #region IElementHandlerFactory Members

        public IElementHandler GetHandlerFor(string elementName, string namespaceUri)
        {
            lock (_handlerMap)
            {
                if (!_handlerMap.ContainsKey(namespaceUri)) return null;
                Dictionary<string, Type> d = _handlerMap[namespaceUri];
                if (!d.ContainsKey(elementName)) return null;
                Type t = d[elementName];
                IElementHandler h = (IElementHandler)t.GetConstructor(new Type[] { }).Invoke(null);
                return h;
            }
        }

        public bool HandlesNamespace(string namespaceUri)
        {
            return _handlerMap.ContainsKey(namespaceUri);
        }


        #endregion

        /*
        public void Process(XmlReader input, XmlWriter output, NameValueCollection parameters)
        {
            XmlFormProcessor proc = new XmlFormProcessor(input, output, parameters, this);
            proc.Process();
        }

        public string Process(String formName, NameValueCollection parameters)
        {
            string formFile = Path.Combine(_formBaseDir, formName);
            
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xtw = new XmlTextWriter(ms, Encoding.UTF8);
            xtw.Namespaces = true;
            //xtw.Settings.Encoding = Encoding.UTF8;
            
            using (FileStream fs = new FileStream(formFile, FileMode.Open, FileAccess.Read))
            {
                XmlTextReader xtr = new XmlTextReader(fs);
                xtr.Namespaces = true;
                XmlFormProcessor proc = new XmlFormProcessor(xtr, xtw, parameters, this);
                try
                {
                    proc.Process();
                }
                catch (Exception ex)
                {
                    string s = string.Format("Error processing form '{0}' (Line: {1}, Position: {2}): {3}", formFile, xtr.LineNumber, xtr.LinePosition, ex.Message);
                    throw new Exception(s, ex);
                }
                xtw.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
         * */

        public static FormProcessorFactory Default
        {
            get { return _defaultInstance; }
        }

    }
}

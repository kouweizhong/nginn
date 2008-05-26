using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XmlProc
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public class ElementHandlerAttribute : System.Attribute
    {
        public string ElementName;
        public string NamespaceUri;
        public bool   InitializeFields = false;
        public ElementHandlerAttribute(string namespaceUri)
        {
            NamespaceUri = namespaceUri;
        }

        public ElementHandlerAttribute(string elementName, string namespaceUri)
        {
            ElementName = elementName;
            NamespaceUri = namespaceUri;
        }
        
        public ElementHandlerAttribute()
        {
        }
    }
}

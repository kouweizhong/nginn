using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace NGinn.Lib.Schema
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TaskParameterAttribute : Attribute
    {
        public bool Required = true;
        public bool DynamicAllowed = true;
        public bool IsInput;
    }


    [Serializable]
    public class TaskParameterInfo
    {
        private string _name;

        public string Name
        {
          get { return _name; }
          set { _name = value; }
        }

        private bool _required;

        public bool Required
        {
            get { return _required; }
            set { _required = value; }
        }
        private bool _dynamicAllowed = true;

        public bool DynamicAllowed
        {
            get { return _dynamicAllowed; }
            set { _dynamicAllowed = value; }
        }

        private Type _paramType;

        public Type ParameterType
        {
            get { return _paramType; }
            set { _paramType = value; }
        }

        private bool _isInput;

        public bool IsInput
        {
            get { return _isInput; }
            set { _isInput = value; }
        }

        public TaskParameterInfo()
        {
        }

        public TaskParameterInfo(string paramName, Type paramType, bool required, bool isInput, bool dynamicAllowed)
        {
            _name = paramName;
            _paramType = paramType;
            _isInput = isInput;
            _required = required;
            _dynamicAllowed = dynamicAllowed;
        }
    }

    [Serializable]
    public class TaskParameterBinding
    {
        /// <summary>
        /// Variable binding type
        /// </summary>
        public enum ParameterBindingType
        {
            //literal value
            Value,
            //Expression that will evaluate to variable value at runtime
            Expr,
            //custom XML
            XML
        }

        private string _propertyName;
        private string _expression;
        private ParameterBindingType _bindingType;

        public TaskParameterBinding()
        {
        }

        public TaskParameterBinding(string propName, ParameterBindingType bindingType, string bindingExpr)
        {
            PropertyName = propName;
            BindingType = bindingType;
            BindingExpression = bindingExpr;
        }

        /// <summary>
        /// Name of the variable that will receive the data
        /// </summary>
        public string PropertyName
        {
            get { return _propertyName; }
            set { _propertyName = value; }
        }
        
        /// <summary>
        /// Binding expression that provides the data for the variable
        /// </summary>
        public string BindingExpression
        {
            get { return _expression; }
            set { _expression = value; }
        }
        
        /// <summary>
        /// Binding type
        /// </summary>
        public ParameterBindingType BindingType
        {
            get { return _bindingType; }
            set { _bindingType = value; }
        }

        protected virtual void WriteXml(XmlWriter xw)
        {
            
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriter xw = XmlWriter.Create(sb);
            WriteXml(xw);
            xw.Flush();
            return sb.ToString();
        }

        public static TaskParameterBinding LoadFromXml(XmlElement binding, XmlNamespaceManager nsmgr)
        {
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            TaskParameterBinding tb = new TaskParameterBinding();
            tb.PropertyName = binding.GetAttribute("parameter");
            XmlElement el = (XmlElement)binding.SelectSingleNode(pr + "value", nsmgr);
            if (el != null)
            {
                tb.BindingType = ParameterBindingType.Value;
                tb.BindingExpression = el.InnerText;
                return tb;
            }
            
            el = (XmlElement) binding.SelectSingleNode(pr + "expr", nsmgr);
            if (el != null)
            {
                tb.BindingType = ParameterBindingType.Expr;
                tb.BindingExpression = el.InnerText;
                return tb;
            }
            throw new ApplicationException("Invalid parameter binding: " + binding.OuterXml);
        }
    }


}

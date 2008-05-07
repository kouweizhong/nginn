using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace NGinn.Lib.Schema
{

    [Serializable]
    public class VariableBinding
    {
        /// <summary>
        /// Variable binding type
        /// </summary>
        public enum VarBindingType
        {
            //Copy one variable to another. Binding expression contains the name of source variable.
            CopyVar,
            //Xslt binding - binding expression is an xslt template that generates xml data for bound variable
            Xslt,
            //Expression that will evaluate to variable value
            Expr
        }

        private string _variableName;
        private string _bindingExpression;
        private VarBindingType _bindingType;

        public VariableBinding()
        {
        }

        public VariableBinding(string targetVar, VarBindingType bindingType, string bindingExpr)
        {
            VariableName = targetVar;
            BindingType = bindingType;
            BindingExpression = bindingExpr;
        }

        /// <summary>
        /// Name of the variable that will receive the data
        /// </summary>
        public string VariableName
        {
            get { return _variableName; }
            set { _variableName = value; }
        }
        
        /// <summary>
        /// Binding expression that provides the data for the variable
        /// </summary>
        public string BindingExpression
        {
            get { return _bindingExpression; }
            set { _bindingExpression = value; }
        }
        
        /// <summary>
        /// Binding type
        /// </summary>
        public VarBindingType BindingType
        {
            get { return _bindingType; }
            set { _bindingType = value; }
        }

        protected virtual void WriteXml(XmlWriter xw)
        {
            xw.WriteStartElement("binding", ProcessDefinition.WORKFLOW_NAMESPACE);
            xw.WriteElementString("variable", ProcessDefinition.WORKFLOW_NAMESPACE, this.VariableName);
            xw.WriteElementString("bindingType", ProcessDefinition.WORKFLOW_NAMESPACE, this.BindingType.ToString());
            if (this.BindingType == VarBindingType.CopyVar)
            {
                xw.WriteElementString("sourceVariable", ProcessDefinition.WORKFLOW_NAMESPACE, this.BindingExpression);
            }
            else
            {
                xw.WriteStartElement("bindingExpr", ProcessDefinition.WORKFLOW_NAMESPACE);
                xw.WriteRaw(this.BindingExpression);
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriter xw = XmlWriter.Create(sb);
            WriteXml(xw);
            xw.Flush();
            return sb.ToString();
        }

    }


}

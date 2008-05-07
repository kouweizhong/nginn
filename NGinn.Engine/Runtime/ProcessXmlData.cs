using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using System.Xml;

namespace NGinn.Engine.Runtime
{
    public class XmlVariableValue
    {
        public VariableDef Variable;
        public List<XmlElement> Value;
    }
        
    class ProcessXmlData
    {
        public List<XmlVariableValue> RetrieveVariablesFromXml(XmlNode parentNode, IList<VariableDef> variables)
        {
            return null;
        }

        public void InsertVariablesIntoXml(XmlNode parentNode, IList<XmlVariableValue> values)
        {

        }

        public void UpdateVariableValues(IList<XmlVariableValue> lhs, IList<XmlVariableValue> rhs, bool replace)
        {
        }


    }
}

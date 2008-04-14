using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using NLog;

namespace NGinn.Engine.Runtime
{
    class XmlProcessingUtil
    {
        public void ValidateXml(string xml)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            XmlSchemaCollection coll = new XmlSchemaCollection();
            
        }
    }
}

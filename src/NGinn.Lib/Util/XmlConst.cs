using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Util
{
    class XmlConst
    {
        public static readonly string XmlSchemaNS = "http://www.w3.org/2001/XMLSchema";
        public static readonly XmlQualifiedName XS_string = new XmlQualifiedName("string", XmlSchemaNS);
        public static readonly XmlQualifiedName XS_int = new XmlQualifiedName("int", XmlSchemaNS);
        public static readonly XmlQualifiedName XS_date = new XmlQualifiedName("date", XmlSchemaNS);
        public static readonly XmlQualifiedName XS_dateTime = new XmlQualifiedName("dateTime", XmlSchemaNS);


    }
}

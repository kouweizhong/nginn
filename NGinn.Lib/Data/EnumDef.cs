using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Data
{
    /// <summary>
    /// Enumeration type definition
    /// </summary>
    [Serializable]
    public class EnumDef : TypeDef
    {
        private SimpleTypeDef _baseType = TypeSet.TYPE_STRING;
        private List<object> _values = new List<object>();

        public IList<object> EnumValues
        {
            get { return _values; }
        }

        public SimpleTypeDef BaseType
        {
            get { return _baseType; }
        }

        public override bool IsSimpleType
        {
            get { return false; }
        }
    }
}

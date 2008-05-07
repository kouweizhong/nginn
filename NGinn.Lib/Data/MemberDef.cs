using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Data
{
    [Serializable]
    public class MemberDef
    {
        private string _name;
        private string _typeName;
        private bool _isArray;
        private bool _isRequired;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        public bool IsArray
        {
            get { return _isArray; }
            set { _isArray = value; }
        }

        public bool IsRequired
        {
            get { return _isRequired; }
            set { _isRequired = value; }
        }
    }

    [Serializable]
    public class StructDef
    {
        private string _name;
        private List<MemberDef> _members = new List<MemberDef>();

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public IList<MemberDef> Members
        {
            get { return _members; }
        }
    }


}

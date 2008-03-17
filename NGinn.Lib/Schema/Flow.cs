using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class Flow
    {
        private NetNode _from;
        private NetNode _to;
        private string _inputCondition;

        public NetNode From
        {
            get { return _from; }
            set { _from = value; }
        }

        public NetNode To
        {
            get { return _to; }
            set { _to = value; }
        }

        public string InputCondition
        {
            get { return _inputCondition; }
            set { _inputCondition = value; }
        }
    }
}

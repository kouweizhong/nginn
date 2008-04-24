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
        private int _evalOrder = -1;
        private string _label;

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

        /// <summary>
        /// Flow label - for informational purposes only
        /// </summary>
        public string Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>
        /// Input condition used in XOR and OR splits.
        /// </summary>
        public string InputCondition
        {
            get { return _inputCondition; }
            set { _inputCondition = value; }
        }

        /// <summary>
        /// Evaluation order in XOR split. Value of -1 identifies the default condition.
        /// </summary>
        public int EvalOrder
        {
            get { return _evalOrder; }
            set { _evalOrder = value; }
        }
    }
}

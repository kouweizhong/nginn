using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    [Serializable]
    public class NetNode
    {
        private string _id;
        private Dictionary<string, Flow> _flowsIn = new Dictionary<string, Flow>();
        private Dictionary<string, Flow> _flowsOut = new Dictionary<string, Flow>();
        private ProcessDefinition _process = null;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// List of flows going out from the node
        /// </summary>
        public ICollection<Flow> FlowsOut
        {
            get { return _flowsOut.Values; }
        }

        /// <summary>
        /// List of flows ordered by evaluation order
        /// </summary>
        public IList<Flow> FlowsOutOrdered
        {
            get
            {
                List<Flow> nl = new List<Flow>(FlowsOut);
                nl.Sort(new Comparison<Flow>(CompareFlowOrder));
                return nl;
            }
        }

        private static int CompareFlowOrder(Flow f1, Flow f2)
        {
            if (f1.EvalOrder < 0) return 1;
            if (f2.EvalOrder < 0) return -1;
            if (f1.EvalOrder < f2.EvalOrder)
                return -1;
            else if (f1.EvalOrder > f2.EvalOrder)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Process the node belongs to
        /// </summary>
        public ProcessDefinition ParentProcess
        {
            get { return _process; }
            set { _process = value; }
        }

        /// <summary>
        /// List of nodes (places or tasks) following current node in the process definition
        /// </summary>
        public IList<NetNode> NodesOut
        {
            get 
            {
                List<NetNode> l = new List<NetNode>();
                foreach (Flow fl in FlowsOut)
                {
                    l.Add(fl.To);
                }
                return l;
            }
        }

        /// <summary>
        /// List of flows leading to current node
        /// </summary>
        public ICollection<Flow> FlowsIn
        {
            get { return _flowsIn.Values; }
        }

        /// <summary>
        /// List of nodes preceding current node.
        /// </summary>
        public IList<NetNode> NodesIn
        {
            get 
            {
                List<NetNode> l = new List<NetNode>();
                foreach (Flow fl in FlowsIn)
                {
                    l.Add(fl.From);
                }
                return l;
            }
        }

        internal void AddFlowIn(Flow f)
        {
            if (f.To != this) throw new Exception("Invalid flow target");
            if (f.From == null) throw new Exception("Missing flow source");
            Flow tm;
            
            if (_flowsIn.TryGetValue(f.From.Id, out tm))
            {
                if (tm != f) throw new Exception("Flow already defined from " + f.From.Id);
            }
            else
            {
                _flowsIn[f.From.Id] = f;
            }
        }

        internal void AddFlowOut(Flow f)
        {
            if (f.From != this) throw new Exception("Invalid flow source");
            if (f.To == null) throw new Exception("Missing flow target");
            Flow tm;
            if (_flowsOut.TryGetValue(f.To.Id, out tm))
            {
                if (tm != f) throw new Exception("Flow already defined to " + f.To.Id);
            }
            else
            {
                _flowsOut[f.To.Id] = f;
            }
        }

        internal virtual bool Validate(IList<ValidationMessage> messages)
        {
            if (_id == null || _id.Length == 0) throw new Exception("Missing node Id");
            return true;
        }
    }

    /// <summary>
    /// Place in the petri net - represents process 'state'.
    /// </summary>
    [Serializable]
    public class Place : NetNode
    {
        private bool _isImplicit = false;

        /// <summary>
        /// Is place implicit - implicit places are used for connecting two tasks if the tasks are connected directly in the process model.
        /// </summary>
        public bool IsImplicit
        {
            get { return _isImplicit; }
            set { _isImplicit = value; }
        }


        public static Place LoadPlace(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string type = el.GetAttribute("type");
            Type t = Type.GetType("NGinn.Lib.Schema." + type);
            if (t == null) throw new Exception("Unknown place type: " + type);
            Place pl = (Place)Activator.CreateInstance(t);
            pl.LoadXml(el, nsmgr);
            return pl;
        }

        internal virtual void LoadXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            Id = el.GetAttribute("id");
        }
        
        /*
        internal static Place LoadPlace(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string type = el.GetAttribute("type");
            Type ptype = Type.GetType("YAWN.Lib.Schema." + type);
            if (ptype == null) throw new Exception("Unrecognized task type: " + type);
            Place pl = (Place)Activator.CreateInstance(ptype);
            pl.ParseXml(el, nsmgr);
            return pl;
        }

        internal void AddOutTransition(Transition tr)
        {
            if (tr.From != this) throw new Exception("Invalid transition start place");
            _transitionsOut.Add(tr);
        }

        internal void AddInTransition(Transition tr)
        {
            if (tr.To != this) throw new Exception("Invalid transition target place");
            _transitionsIn.Add(tr);
        }

        */
    }

    [Serializable]
    public class StartPlace : Place
    {
    }

    [Serializable]
    public class EndPlace : Place
    {
    }
}

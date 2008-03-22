using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Schema
{
    /// <summary>
    /// Place split type or join synchronization type
    /// </summary>
    public enum JoinType
    {
        AND,  //AND split, AND synchronization - default
        XOR,  //XOR split, XOR synchronization
        OR    //OR split, OR synchronization
    }

    [Serializable]
    public abstract class Task : NetNode
    {
        
        private JoinType _joinType;
        private JoinType _splitType;
        
        public JoinType JoinType
        {
            get { return _joinType; }
        }

        public JoinType SplitType
        {
            get { return _splitType; }
        }

        internal static Task LoadTask(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string t = el.GetAttribute("type");
            Type tp = Type.GetType("NGinn.Lib.Schema." + t);
            if (tp == null) throw new Exception("Unknown task type: " + t);
            Task tsk = (Task)Activator.CreateInstance(tp);
            tsk.LoadXml(el, nsmgr);
            return tsk;
        }

        internal virtual void LoadXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            Id = el.GetAttribute("id");
        }

        /// <summary>
        /// Tells whether transition is immediate (executes immediately in single transaction)
        /// If the transition is not immediate, system will initiate it and wait for completion.
        /// </summary>
        public abstract bool IsImmediate
        {
            get;
        }



    }
}

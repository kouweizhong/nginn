using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using NGinn.Lib.Data;

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
        private List<VariableDef> _taskVariables = new List<VariableDef>();
        private List<VariableBinding> _inputBindings = new List<VariableBinding>();
        private List<VariableBinding> _outputBindings = new List<VariableBinding>();
        private bool _isMultiInstance = false;

        /// <summary>
        /// Test if this is a multi-instance task
        /// </summary>
        public bool IsMultiInstance
        {
            get { return _isMultiInstance; }
        }

        public string MultiInstanceSplitQuery
        {
            get { return null; }
        }

        public string MultiInstanceResultVariable
        {
            get { return null; }
        }

        public JoinType JoinType
        {
            get { return _joinType; }
            set { _joinType = value; }
        }

        public JoinType SplitType
        {
            get { return _splitType; }
            set { _splitType = value; }
        }

        /// <summary>
        /// List of all task variables (in, out and local)
        /// </summary>
        public IList<VariableDef> TaskVariables
        {
            get { return _taskVariables; }
        }

        /// <summary>
        /// Return variable with specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VariableDef GetVariable(string name)
        {
            foreach (VariableDef vd in _taskVariables)
            {
                if (vd.Name == name) return vd;
            }
            return null;
        }

        /// <summary>
        /// Add new variable definition
        /// </summary>
        /// <param name="vd"></param>
        public void AddTaskVariable(VariableDef vd)
        {
            if (GetVariable(vd.Name) != null) throw new ApplicationException("Variable already defined: " + vd.Name);
            _taskVariables.Add(vd);
        }

        /// <summary>
        /// Input variable bindings
        /// </summary>
        public IList<VariableBinding> InputBindings
        {
            get { return _inputBindings; }
        }

        /// <summary>
        /// Output data bindings
        /// </summary>
        public IList<VariableBinding> OutputBindings
        {
            get { return _outputBindings; }
        }


        /// <summary>
        /// Todo: cancel set loading
        /// </summary>
        /// <param name="el"></param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        internal static Task LoadTask(XmlElement el, XmlNamespaceManager nsmgr)
        { 
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";

            string t = el.GetAttribute("type");
            Type tp = Type.GetType("NGinn.Lib.Schema." + t);
            if (tp == null) throw new Exception("Unknown task type: " + t);
            Task tsk = (Task)Activator.CreateInstance(tp);
            t = el.GetAttribute("joinType");
            tsk.JoinType = (JoinType) Enum.Parse(typeof(JoinType), t);
            t = el.GetAttribute("splitType");
            tsk.SplitType = (JoinType)Enum.Parse(typeof(JoinType), t);
            XmlElement data = (XmlElement) el.SelectSingleNode(pr + "data-definition", nsmgr);
            List<VariableDef> variables = new List<VariableDef>();
            List<VariableBinding> inputBind = new List<VariableBinding>();
            List<VariableBinding> outputBind = new List<VariableBinding>();
            SchemaUtil.LoadDataSection(data, nsmgr, variables, inputBind, outputBind);
            foreach (VariableDef vd in variables) tsk.AddTaskVariable(vd);
            foreach (VariableBinding vb in inputBind) tsk.InputBindings.Add(vb);
            foreach (VariableBinding vb in outputBind) tsk.OutputBindings.Add(vb);
            
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

        /// <summary>
        /// Get XSLT for creating task input data
        /// </summary>
        /// <returns></returns>
        public void GetInputBindingXslt()
        {
            throw new NotImplementedException();
        }

        

        /// <summary>
        /// Get XSLT for creating task output data
        /// </summary>
        /// <returns></returns>
        public string GetOutputBindingXslt()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get XSD for task input xml
        /// </summary>
        /// <returns></returns>
        public string GetInputDataSchema()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the definition of task input data structure
        /// </summary>
        /// <returns></returns>
        public StructDef GetTaskInputDataSchema()
        {
            if (ParentProcess == null) throw new Exception();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ParentProcess.DataTypes;
            foreach (VariableDef vd in TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    sd.Members.Add(vd);
                }
            }
            return sd;
        }

        /// <summary>
        /// Get the definition of task output data
        /// </summary>
        /// <returns></returns>
        public StructDef GetTaskOutputDataSchema()
        {
            if (ParentProcess == null) throw new Exception();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ParentProcess.DataTypes;
            foreach (VariableDef vd in TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.Out || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    sd.Members.Add(vd);
                }
            }
            return sd;
        }

        public StructDef GetTaskInternalDataSchema()
        {
            if (ParentProcess == null) throw new Exception();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ParentProcess.DataTypes;
            foreach (VariableDef vd in TaskVariables)
            {
                sd.Members.Add(vd);
            }
            return sd;
        }
    }
}

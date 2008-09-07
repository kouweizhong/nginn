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
        private JoinType _joinType = JoinType.AND;
        private JoinType _splitType = JoinType.AND;
        private List<VariableDef> _taskVariables = new List<VariableDef>();
        private List<VariableBinding> _inputBindings = new List<VariableBinding>();
        private List<VariableBinding> _outputBindings = new List<VariableBinding>();
        
        private bool _isMultiInstance = false;
        private List<string> _cancelSet = new List<string>();
        private List<string> _orJoinChecklist = new List<string>();
        private List<TaskParameterBinding> _parameterBindings = new List<TaskParameterBinding>();
        private string _multiInstQuery;
        private string _multiInstVariable;
        private string _multiInstResultVariable;
        /// <summary>
        /// Test if this is a multi-instance task
        /// </summary>
        public bool IsMultiInstance
        {
            get { return _isMultiInstance; }
        }

        /// <summary>
        /// Expression that will return an IEnumerable that will be used
        /// as data provider for multi-instance tasks
        /// </summary>
        public string MultiInstanceSplitQuery
        {
            get { return _multiInstQuery; }
            set { _multiInstQuery = value; }
        }

        /// <summary>
        /// Variable that will hold data element returned by the multi instance split query
        /// </summary>
        public string MultiInstanceInputVariable
        {
            get { return _multiInstVariable; }
            set { _multiInstVariable = value; }
        }

        /// <summary>
        /// Name of variable that will receive multi-instance results
        /// </summary>
        public string MultiInstanceResultVariable
        {
            get { return _multiInstResultVariable; }
            set { _multiInstResultVariable = value; }
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
            if (t != null && t.Length > 0) tsk.JoinType = (JoinType) Enum.Parse(typeof(JoinType), t);
            t = el.GetAttribute("splitType");
            if (t != null && t.Length > 0) tsk.SplitType = (JoinType)Enum.Parse(typeof(JoinType), t);
            t = el.GetAttribute("multiInstance");
            if (t != null && t.Length > 0) tsk._isMultiInstance = Boolean.Parse(t);
            XmlElement paramBinds = (XmlElement)el.SelectSingleNode(pr + "parameters", nsmgr);
            if (paramBinds == null) throw new Exception();
            List<TaskParameterBinding> bindingsList = new List<TaskParameterBinding>();
            SchemaUtil.LoadTaskParameterBindings(paramBinds, nsmgr, tsk._parameterBindings);
            XmlElement data = (XmlElement) el.SelectSingleNode(pr + "data-definition", nsmgr);
            List<VariableDef> variables = new List<VariableDef>();
            List<VariableBinding> inputBind = new List<VariableBinding>();
            List<VariableBinding> outputBind = new List<VariableBinding>();
            SchemaUtil.LoadDataSection(data, nsmgr, variables, inputBind, outputBind);
            foreach (VariableDef vd in variables) tsk.AddTaskVariable(vd);
            foreach (VariableBinding vb in inputBind) tsk.InputBindings.Add(vb);
            foreach (VariableBinding vb in outputBind) tsk.OutputBindings.Add(vb);
            if (tsk.IsMultiInstance)
            {
                XmlElement multInst = (XmlElement) data.SelectSingleNode(pr + "multi-instance", nsmgr);
                if (multInst == null) throw new ApplicationException("Missing multi instance binding definition for task " + tsk.Id);
                XmlElement tmp = (XmlElement) multInst.SelectSingleNode(pr + "foreach", nsmgr);
                if (tmp == null) throw new Exception("Missing 'foreach' in multi-instance");
                tsk.MultiInstanceSplitQuery = tmp.GetAttribute("query");
                tsk.MultiInstanceInputVariable = tmp.GetAttribute("variableName");
                tsk.MultiInstanceResultVariable = SchemaUtil.GetXmlElementText(multInst, pr + "resultsVariable", nsmgr);
            }
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
                if (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    sd.Members.Add(vd);
                }
                else
                {
                    VariableDef vd2 = new VariableDef(vd); vd2.IsRequired = false;
                    sd.Members.Add(vd2);
                }
            }
            return sd;
        }

        /// <summary>
        /// Task's cancel set. Contains a list of ids of places.
        /// </summary>
        public IList<string> CancelSet
        {
            get { return _cancelSet;  }
        }

        /// <summary>
        /// OR join checklist. List of places that should be checked for 
        /// tokens when performing OR join. This will become redundant 
        /// after OR-join analysis will be implemented.
        /// </summary>
        public IList<string> ORJoinChecklist
        {
            get
            {
                return _orJoinChecklist;
            }
        }

        public virtual TaskParameterInfo[] GetTaskParameters()
        {
            return null;
        }

        public virtual IList<TaskParameterBinding> ParameterBindings
        {
            get { return _parameterBindings; }
        }

        protected virtual bool RequireInputParameter(string name)
        {
            foreach (TaskParameterBinding tb in ParameterBindings)
            {
                if (tb.PropertyName == name) return true;
            }
            return false;
        }

        internal override bool Validate(IList<ValidationMessage> messages)
        {
            bool b = base.Validate(messages);
            if (!b) return false;
            if (this.JoinType == JoinType.OR)
            {
                if (this.ORJoinChecklist.Count == 0)
                {
                    messages.Add(new ValidationMessage(true, Id, "OR-join task has no or-join checklist. In this version of NGinn you MUST specify the checklist for each OR-join"));
                }
            }
            Dictionary<string, TaskParameterBinding> bDict = new Dictionary<string, TaskParameterBinding>();
            foreach (TaskParameterBinding tbi in ParameterBindings)
            {
                bDict[tbi.PropertyName] = tbi;
            }
            TaskParameterInfo[] tpis = GetTaskParameters();
            if (tpis != null)
            {
                foreach (TaskParameterInfo tpi in this.GetTaskParameters())
                {
                    if (tpi.Required)
                    {
                        if (!bDict.ContainsKey(tpi.Name))
                        {
                            messages.Add(new ValidationMessage(true, Id, "Missing required task parameter binding: " + tpi.Name));
                        }
                    }
                }
            }
            return messages.Count > 0 ? false : true;
        }
    }
}

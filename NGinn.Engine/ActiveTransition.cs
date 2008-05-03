using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using System.Xml;
using System.Xml.Schema;
using NGinn.Engine.Runtime;

namespace NGinn.Engine
{
    public enum TransitionStatus
    {
        ENABLED,    //transition task created & offered (also for deferred choice to be selected)
        STARTED,    //transition task started (deferred choice alternative has been selected)
        COMPLETED,  //task finished
        CANCELLED,  //task cancelled (other transition sharing the same token fired)
        ERROR,      //task did not complete due to error
    }

    /// <summary>
    /// Represents an 'active' counterpart of workflow transition (Task). Task is a definition of an activity, and
    /// ActiveTransition subclasses define instances of particular task with logic for implementing them.
    /// </summary>
    [Serializable]
    public abstract class ActiveTransition
    {
        /// <summary>Process instance Id</summary>
        public string ProcessInstanceId;
        /// <summary>Correlation id. Warning: it should be unique in scope of a single process. 
        /// CorrelationId should be present after task has been initiated.</summary>
        private string _correlationId;
        /// <summary>Id of task in a process</summary>
        public string TaskId;
        public IList<string> Tokens = new List<string>();
        public TransitionStatus Status;
        /// <summary>If active transitions share some tokens, they will have the same SharedId. If one of 
        /// shared transitions completes, it will cancell all other transitions with the same SharedId
        /// </summary>
        public string SharedId;
        [NonSerialized]
        protected ProcessInstance _processInstance;
        [NonSerialized]
        protected Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        private bool _activated = false;
        [NonSerialized]
        private XmlDocument _taskDataDoc;
        /// <summary>Serialized task xml data</summary>
        private string _taskDataXml;
        
        public ActiveTransition(Task tsk, ProcessInstance pi)
        {
            this.Status = TransitionStatus.ENABLED;
            this.TaskId = tsk.Id;
            this._processInstance = pi;
            this.ProcessInstanceId = pi.InstanceId;
        }

        public virtual void SetProcessInstance(ProcessInstance pi)
        {
            if (this.ProcessInstanceId != pi.InstanceId) throw new ApplicationException("Invalid process instance ID");
            this._processInstance = pi;
        }

        /// <summary>
        /// Task correlation id. Uniquely identifies the task instance.
        /// </summary>
        public string CorrelationId
        {
            get { return _correlationId; }
            set { ActivationRequired(false); _correlationId = value; }
        }

        /// <summary>
        /// Serialized form of task data xml.
        /// </summary>
        public string TaskDataXml
        {
            get { return _taskDataXml; }
            set { _taskDataXml = value; }
        }

        /// <summary>
        /// Task xml data - available when activated
        /// </summary>
        public XmlDocument TaskData
        {
            get { ActivationRequired(true); return _taskDataDoc; }
        }

        /// <summary>
        /// Return xml node containing task variables - available when activated
        /// </summary>
        public XmlNode TaskVariablesRoot
        {
            get { ActivationRequired(true); return _taskDataDoc.DocumentElement; }
        }

        /// <summary>
        /// Called after deserialization
        /// </summary>
        public virtual void Activate()
        {
            if (_processInstance == null) throw new ApplicationException("Process instance not set (call SetProcessInstance before activating)");
            if (_taskDataXml != null)
            {
                _taskDataDoc = new XmlDocument();
                _taskDataDoc.LoadXml(_taskDataXml);
            }
            _activated = true;
        }

        /// <summary>
        /// Called before serialization
        /// </summary>
        public virtual void Passivate()
        {
            if (_taskDataDoc != null)
                _taskDataXml = _taskDataDoc.OuterXml;
            _processInstance = null;
            _activated = false;
        }

        /// <summary>
        /// Current transition's task definition
        /// </summary>
        protected Task ProcessTask
        {
            get { ActivationRequired(true); return _processInstance.Definition.GetTask(TaskId); }
        }

        

        /// <summary>
        /// Set task input xml
        /// </summary>
        /// <param name="xml"></param>
        public virtual void SetTaskInputXml(string xml)
        {
            ActivationRequired(true);
            XmlSchemaSet validationSchemas = XmlProcessingUtil.GetTaskInputSchemas(this.ProcessTask);
            List<XmlValidationMessage> msgs = new List<XmlValidationMessage>();
            bool b = XmlProcessingUtil.ValidateXml(xml, validationSchemas, msgs);
            if (!b) throw new ApplicationException("Input data validation failed");
            XmlDocument d1 = new XmlDocument();
            d1.LoadXml(xml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(d1.NameTable);
            IDictionary<string, IList<XmlElement>> values = XmlProcessingUtil.RetrieveVariablesFromXml(d1.DocumentElement, ProcessTask.TaskVariables, nsmgr);
            //now build the task xml
            
            XmlDocument taskData = new XmlDocument();
            taskData.AppendChild(taskData.CreateElement("taskData"));
            foreach (VariableDef vd in this.ProcessTask.TaskVariables)
            {
                IList<XmlElement> variableData;
                if (!values.TryGetValue(vd.Name, out variableData) || variableData.Count == 0)
                {
                    if (vd.VariableUsage == VariableDef.Usage.Required)
                    {
                        if (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut)
                            throw new ApplicationException("Missing required input variable: " + vd.Name);
                    }
                    if (vd.DefaultValueExpr != null && vd.DefaultValueExpr.Length > 0)
                    {
                        XmlElement vel = taskData.CreateElement(vd.Name);
                        vel.InnerXml = vd.DefaultValueExpr;
                    }
                }
            }
            XmlProcessingUtil.InsertVariablesIntoXml(taskData.DocumentElement, values, ProcessTask.TaskVariables);
            log.Info("Task data xml: {0}", taskData.OuterXml);
            _taskDataDoc = taskData;
        }

        public virtual string GetTaskOutputXml()
        {
            if (this.Status != TransitionStatus.COMPLETED) throw new ApplicationException("Transition is not completed");
            return null;
        }
        

        

        /// <summary>
        /// Initiate task (start the transition).
        /// If the transition is immediate, this operation will execute the task.
        /// If the transition is not immediate, this will initiate the transition.
        /// Subclasses should override this function, but should always call base.InitiateTask()
        /// </summary>
        public virtual void InitiateTask()
        {
            ActivationRequired(true);
            if (this.Tokens.Count == 0) throw new Exception("No input tokens");
        }

        /// <summary>
        /// Check if task is immediate
        /// </summary>
        public virtual bool IsImmediate
        {
            get
            {
                return this.ProcessTask.IsImmediate;
            }
        }

        /// <summary>
        /// Execute an immediate task
        /// </summary>
        public virtual void ExecuteTask()
        {
            ActivationRequired(true);
            if (!IsImmediate) throw new ApplicationException("Execute is allowed only for immediate task");
        }

        /// <summary>
        /// Invoked by runtime to cancel an active transition
        /// </summary>
        public virtual void CancelTask()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED && Status != TransitionStatus.STARTED)
                throw new ApplicationException("Cannot cancel task - status invalid");
            this.Status = TransitionStatus.CANCELLED;
        }

        /// <summary>
        /// Invoked by runtime after transition has completed.
        /// </summary>
        public virtual void TaskCompleted()
        {
            ActivationRequired(true);
            if (this.Status != TransitionStatus.ENABLED && this.Status != TransitionStatus.STARTED)
                throw new ApplicationException("Cannot complete task - status invalid");
            this.Status = TransitionStatus.COMPLETED;
        }

        protected void ActivationRequired(bool activated)
        {
            if (_activated != activated)
            {
                throw new ApplicationException(activated ? "Task must be activated" : "Task must be passivated");
            }
        }
    }
}

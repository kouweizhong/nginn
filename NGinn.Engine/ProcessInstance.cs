using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using Spring.Context;
using NGinn.Engine.Services;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NGinn.Engine.Runtime;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;
using NGinn.Engine.Runtime.Tasks;
using System.Collections;

namespace NGinn.Engine
{
    

    /// <summary>
    /// Enumeration of possible process instance statuses
    /// </summary>
    public enum ProcessStatus
    {
        Ready = 1,
        Waiting = 2,
        Finished = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Represents an instance of business process. 
    /// Warning: ProcessInstance objects are not thread safe. That is, it is safe to update multiple instances of ProcessInstance
    /// in parallel, but two threads shouldn't update the same ProcessInstance object. 
    /// </summary>
    [Serializable]
    public class ProcessInstance : IProcessTransitionCallback, INGinnPersistent
    {
        [NonSerialized]
        private Logger log = LogManager.GetCurrentClassLogger();
        private string _instId;
        private string _definitionId;
        [NonSerialized]
        private ProcessDefinition _definition;
        private int _persistedVersion;
        [NonSerialized]
        private bool _activated = false;
        [NonSerialized]
        private INGEnvironmentContext _environment;
        /// <summary>map: correlation id->transition</summary>
        private IDictionary<string, TaskShell> _activeTransitions = new Dictionary<string, TaskShell>();
        /// <summary>helper map: task id -> list of active instances of the task</summary>
        private ProcessStatus _status;
        /// <summary>transition id generator</summary>
        private int _transitionNumber = 0;
        /// <summary>Instance data</summary>
        private DataObject _processInstanceData = new DataObject();
        
        private string _correlationId;
        /// <summary>map place Id -> number of tokens</summary>
        private Dictionary<string, int> _currentMarking = new Dictionary<string, int>();
        /// <summary>id of user that started the process</summary>
        private string _startedBy;
        /// <summary>process started date</summary>
        private DateTime _startDate = DateTime.MinValue;

        public ProcessInstance()
        {
            _status = ProcessStatus.Ready;
        }

        /// <summary>
        /// Id of process definition
        /// </summary>
        public string ProcessDefinitionId
        {
            get { return _definitionId; }
            set { _definitionId = value; }
        }

        /// <summary>
        /// Access the process definition of current process.
        /// Can be called only for activated process instances.
        /// </summary>
        public ProcessDefinition Definition
        {
            get { return _definition; }
        }

        /// <summary>
        /// Process instance id
        /// </summary>
        public string InstanceId
        {
            get { return _instId; }
            set 
            { 
                _instId = value;
                log = LogManager.GetLogger(string.Format("ProcessInstance.{0}", value));
            }
        }

        /// <summary>
        /// Process correlation id.
        /// </summary>
        public string CorrelationId
        {
            get { return _correlationId; }
            set { _correlationId = value; }
        }

        private string _externalId;

        /// <summary>
        /// External identification field. You can assign any value to it
        /// and use it for process lookup.
        /// </summary>
        public string ExternalId
        {
            get { return _externalId; }
            set { _externalId = value; }
        }

        /// <summary>
        /// Current status of the process
        /// </summary>
        public ProcessStatus Status
        {
            get { return _status; }
        }

        /// <summary>
        /// Persistence version number
        /// </summary>
        public int PersistedVersion
        {
            get { return _persistedVersion; }
            set { _persistedVersion = value; }
        }

        /// <summary>
        /// Environment that hosts the process instance. Warning: this property should be set before Activating
        /// process instance
        /// </summary>
        public INGEnvironmentContext Environment
        {
            get { return _environment; }
            set { _environment = value; }
        }

        /// <summary>Id of user who started the process</summary>
        public string StartedBy
        {
            get { return _startedBy; }
            set { _startedBy = value; }
        }

        /// <summary>Process start date</summary>
        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        
        /// <summary>
        /// Allocate next active transition Id.
        /// </summary>
        /// <returns></returns>
        internal string GetNextTransitionId()
        {
            
            int n;
            lock(this)
            {
                n = _transitionNumber;
                _transitionNumber++;
            }
            return string.Format("{0}.{1}", _instId, n);
        }



        /// <summary>
        /// Return number of free (unallocated) tokens in given place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public int GetNumFreeTokens(string placeId)
        {
            int n = 0;
            return _currentMarking.TryGetValue(placeId, out n) ? n : 0;
        }

        /// <summary>
        /// Get total number of tokens (free + allocated for 'STARTED' tasks) in
        /// specified place.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public int GetTotalTokens(string placeId)
        {
            lock (this)
            {
                int n = GetNumFreeTokens(placeId);
                foreach (Task tsk in Definition.GetPlace(placeId).NodesOut)
                {
                    TaskShell ts = GetActiveInstanceOfTask(tsk.Id);
                    if (ts != null && ts.Status == TransitionStatus.STARTED)
                    {
                        if (ts.AllocatedPlaces.Contains(placeId)) 
                            n++;
                    }
                }
                return n;
            }
        }

        /// <summary>
        /// Add one token to a place
        /// </summary>
        /// <param name="placeId"></param>
        public void AddToken(string placeId)
        {
            lock (this)
            {
                int n = 0;
                if (_currentMarking.TryGetValue(placeId, out n)) _currentMarking.Remove(placeId);
                n++;
                _currentMarking[placeId] = n;
                if (Status == ProcessStatus.Waiting) _status = ProcessStatus.Ready;
                if (placeId == Definition.Finish.Id)
                {
                    OnEndPlaceReached();
                }
            }
        }

        /// <summary>
        /// Remove a token from place
        /// </summary>
        /// <param name="placeId"></param>
        private void RemoveToken(string placeId)
        {
            lock (this)
            {
                int n = GetNumFreeTokens(placeId);
                if (n <= 0) throw new Exception("No tokens in " + placeId);
                _currentMarking.Remove(placeId);
                _currentMarking[placeId] = --n;
                if (Status == ProcessStatus.Waiting) _status = ProcessStatus.Ready;
            }
        }

        /// <summary>
        /// Invoked when process starts (first token is added)
        /// </summary>
        private void OnProcessStarted()
        {
            ProcessStarted ps = new ProcessStarted();
            ps.InstanceId = InstanceId;
            ps.DefinitionId = ProcessDefinitionId;
            ps.CorrelationId = CorrelationId;
            NotifyProcessEvent(ps);
        }

        /// <summary>
        /// Send a notification about process event
        /// </summary>
        /// <param name="pe"></param>
        internal void NotifyProcessEvent(ProcessEvent pe)
        {
            if (Environment.MessageBus != null)
            {
                string topic = string.Format("ProcessInstance.{0}.{1}", pe.GetType().Name, InstanceId);
                Environment.MessageBus.Notify("ProcessInstance." + InstanceId, topic, pe, true);
            }
        }

        /// <summary>
        /// Add new token to process instance
        /// </summary>
        /// <param name="tok"></param>
        public void CreateStartToken()
        {
            if (!_activated) throw new Exception("Process instance not activated");
            lock (this)
            {
                AddToken(Definition.Start.Id);
                _status = ProcessStatus.Ready;
                if (GetNumFreeTokens(Definition.Start.Id) == 1)
                {
                    OnProcessStarted();
                }
            }
        }

        /// <summary>
        /// Return process data for data binding
        /// </summary>
        /// <returns></returns>
        protected internal IDataObject GetProcessDataSource()
        {
            DataObject dob = new DataObject(GetProcessVariablesContainer());
            dob.Set("_processDef", null, Definition);
            dob.Set("_instance", null, this);
            return dob;
        }

        /// <summary>
        /// Initialize new script execution context for this process instance
        /// </summary>
        /// <returns></returns>
        protected internal IProcessScript CreateProcessScriptContext()
        {
            IProcessScript ps = Environment.ScriptManager.GetProcessScript(Definition);
            ps.Instance = this;
            ps.EnvironmentContext = this.Environment;
            ps.ProcessData = GetProcessDataSource();
            return ps;
            
        }

        

        /// <summary>
        /// Get definition of process instance data (in + local + out variables)
        /// </summary>
        /// <returns></returns>
        protected StructDef GetProcessInternalDataSchema()
        {
            StructDef sd = new StructDef();
            sd.ParentTypeSet = Definition.DataTypes;
            foreach (VariableDef vd in Definition.ProcessVariables)
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
        /// Return process instance output data
        /// Can be called only if Status == Finished
        /// </summary>
        /// <returns></returns>
        public DataObject GetProcessOutputData()
        {
            if (Status != ProcessStatus.Finished) throw new ApplicationException("Process must be finished to get output data");
            if (!_activated) throw new ApplicationException("Not activated");

            StructDef sd = this.Definition.GetProcessOutputDataSchema();
            DataObject dob = new DataObject(sd);
            IDataObject src = this.GetProcessVariablesContainer();
            foreach (VariableDef vd in Definition.ProcessVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.InOut || vd.VariableDir == VariableDef.Dir.Out)
                {
                    object obj = src[vd.Name];
                    dob.Set(vd.Name, null, obj);
                }
            }
            dob.Validate();
            return dob;
        }
        /// <summary>
        /// Set process input data
        /// </summary>
        /// <param name="data"></param>
        public void SetProcessInputData(IDataObject data)
        {
            StructDef procInput = Definition.GetProcessInputDataSchema();
            data.Validate(procInput);
            DataObject dob = new DataObject();
            
            IProcessScript ctx = CreateProcessScriptContext();
            
            
            foreach (VariableDef vd in Definition.ProcessVariables)
            {
                if (data.ContainsKey(vd.Name))
                {
                    dob[vd.Name] = data[vd.Name];
                }
                else
                {
                    if (vd.DefaultValueExpr == null || vd.DefaultValueExpr.Length == 0)
                    {
                        if (vd.IsRequired && (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut))
                            throw new ApplicationException("Missing required input variable: " + vd.Name);
                    }
                    else
                    {
                        object val = ctx.GetDefaultVariableValue(vd.Name);
                        dob[vd.Name] = val;
                    }
                }
            }
            StructDef internalSchema = GetProcessInternalDataSchema();
            dob.Validate(internalSchema);
            _processInstanceData = new DataObject();
            _processInstanceData["variables"] = dob;
            _processInstanceData["instanceInfo"] = new DataObject();
        }

        /// <summary>
        /// Validate structure of process variables
        /// </summary>
        private void ValidateProcessInternalData()
        {
            StructDef internalSchema = GetProcessInternalDataSchema();
            IDataObject dob = GetProcessVariablesContainer();
            dob.Validate(internalSchema);
        }

        /// <summary>
        /// Initialize process input data
        /// Should be called on new process instance, before any tokens are processed.
        /// This function validates input data and inserts it into process data xml
        /// </summary>
        /// <param name="inputXml"></param>
        public void SetProcessInputData(string inputXml)
        {
            if (!_activated) throw new Exception("Not activated");
            XmlReader xr = XmlReader.Create(new StringReader(inputXml));
            xr.MoveToContent();
            DataObject dob = DataObject.ParseXmlElement(xr);
            SetProcessInputData(dob);
        }

        

        /// <summary>
        /// Return node where process variables are kept
        /// </summary>
        /// <returns></returns>
        public IDataObject GetProcessVariablesContainer()
        {
            return (IDataObject)_processInstanceData["variables"];
        }


        /// <summary>
        /// executes one or more process steps
        /// returns true - if process could continue
        /// returns false - if process cannot continue
        /// </summary>
        /// <returns></returns>
        public bool Kick()
        {
            bool b = KickTokens();
            if (Status == ProcessStatus.Ready || Status == ProcessStatus.Waiting)
            {
                _status = b ? ProcessStatus.Ready : ProcessStatus.Waiting;
            }
            return Status == ProcessStatus.Ready;
        }

        /// <summary>
        /// Passivate is called before persisting the process instance data
        /// </summary>
        public void Passivate()
        {
            log.Info("Passivating");
            foreach (TaskShell ts in _activeTransitions.Values)
            {
                ts.Passivate();
            }
            _definition = null;
            _environment = null;
            _activated = false;
        }
        
        /// <summary>
        /// Activate is called after process instance is deserialized, but before any operations
        /// are performed on it.
        /// Before Activation:
        /// * Environment should be set
        /// * Process state should be restored
        /// </summary>
        public void Activate()
        {
            if (_activated) throw new Exception("Process instance already activated");
            if (Environment == null) throw new Exception("Environment not initialized. Please set the 'Environment' property");
            log = LogManager.GetLogger(string.Format("ProcessInstance.{0}", InstanceId));
            
            _definition = Environment.PackageRepository.GetProcess(ProcessDefinitionId);
            
            foreach (TaskShell at in _activeTransitions.Values)
            {
                at.SetProcessInstance(this);
                at.ParentCallback = this;
                at.EnvironmentContext = this.Environment;
                at.Activate();
            }
            _activated = true;
        }

        


        /// <summary>
        /// Return active transitions (transitions that have not completed yet)
        /// </summary>
        /// <returns></returns>
        private IList<TaskShell> GetActiveTransitions()
        {
            List<TaskShell> lst = new List<TaskShell>();
            foreach (TaskShell at in _activeTransitions.Values)
            {
                if (at.Status == TransitionStatus.ENABLED ||
                    at.Status == TransitionStatus.STARTED)
                {
                    lst.Add(at);
                }
            }
            return lst;
        }

        /// <summary>
        /// Current number of tokens in the process
        /// </summary>
        /// <returns></returns>
        public int GetTotalProcessTokens()
        {
            lock (this)
            {
                int n = 0;
                foreach (int x in _currentMarking.Values) n += x;
                foreach (TaskShell ts in GetActiveTransitions())
                {
                    if (ts.Status == TransitionStatus.STARTED)
                    {
                        Debug.Assert(ts.AllocatedPlaces.Count > 0);
                        n += ts.AllocatedPlaces.Count;
                    }
                }
                return n;
            }
        }

        

        /// <summary>
        /// Invoked when a token has reached process end place
        /// </summary>
        /// <param name="tok"></param>
        private void OnEndPlaceReached()
        {
            Place pl = Definition.Finish;
            int ftoks = GetNumFreeTokens(pl.Id);
            int total = GetTotalProcessTokens();
            Debug.Assert(total >= ftoks);
            log.Info("End place reached - {0} tokens remaining", total - ftoks);
            if (total - ftoks == 0)
            {
                log.Info("No more tokens alive - process has finished");
                Debug.Assert(GetActiveTransitions().Count == 0);
                OnProcessFinished();
            }
        }


        /// <summary>
        /// Invoked when process has finished
        /// </summary>
        private void OnProcessFinished()
        {
            Debug.Assert(GetTotalProcessTokens() - GetTotalTokens(Definition.Finish.Id) == 0);
            Debug.Assert(GetActiveTransitions().Count == 0);
            _status = ProcessStatus.Finished;
            ProcessFinished pf = new ProcessFinished();
            pf.InstanceId = InstanceId;
            pf.DefinitionId = ProcessDefinitionId;
            pf.CorrelationId = CorrelationId;
            NotifyProcessEvent(pf);
        }

        


        /// <summary>
        /// Kick a 'READY' token. 
        /// Implementation:
        /// 1. We assume each task can be enabled only once (only one active instance), no matter
        /// how many input tokens it has 
        /// 2. In future we might allow more than 1 active instance, however this might introduce some difficulties
        /// 3. Kicking the token - first of all, select the READY token (t0)
        /// 3.1 If the token's place has at least one other token, mark the token (t0) as WAITING
        ///     This is because tokens are indistinguishable, so adding one more token does not change anything
        ///     Wow, it means that we don't have to identify the tokens until they don't convey any information
        /// 3.2 So now the token's place contains only one token (t0)
        /// 3.2.1 For each output task of the token
        ///         - if the task is active, skip it
        /// 3.2.1.1 The task is not active
        ///         - check if it can be enabled (has required input tokens)
        ///         - if can be enabled, enable it
        ///         - shared id - is the id of input place the task shares with other
        /// </summary>
        /// <param name="tok"></param>
        protected bool KickTokens()
        {
            bool enabled = false;
            log.Debug("[KickTokens start]");
            foreach (Task tsk in Definition.Tasks)
            {
                lock (this)
                {
                    TaskShell ts = GetActiveInstanceOfTask(tsk.Id);
                    if (ts != null)
                    {
                        log.Debug("Skipping already active transition {0}", ts.ToString());
                        continue;
                    }
                    if (!CanEnableTransition(tsk.Id))
                    {
                        log.Info("Transition {0} cannot be enabled, skipping", tsk.Id);
                        continue;
                    }
                    log.Info("Enabling transition {0}", tsk.Id);
                    EnableTransition(tsk.Id);
                    enabled = true;
                }
            }
            log.Debug("[KickTokens end. Returning {0}]", enabled);
            return enabled;
        }

        /// <summary>
        /// Enable specified transition
        /// </summary>
        /// <param name="taskId"></param>
        private TaskShell EnableTransition(string taskId)
        {
            lock (this)
            {
                List<string> enablingPlaces;
                bool b = CanEnableTransition(taskId, out enablingPlaces);
                if (!b) throw new Exception("Task cannot be enabled: " + taskId);
                TaskShell ts = CreateActiveTransitionForTask(Definition.GetTask(taskId));
                List<string> shids = Definition.GetSharedInputPlaces(taskId);
                if (shids.Count == 1) ts.SharedId = shids[0];
                ts.Activate();
                log.Info("Created new transition for task {0}: {1}", taskId, ts.CorrelationId);
                _activeTransitions.Add(ts.CorrelationId, ts);
                ts.Status = TransitionStatus.ENABLED;
                ts.InitiateTask(GetProcessDataSource());
                log.Info("Task {0} enabled", ts.ToString());
                return ts;
            }
        }


        private TaskShell CreateActiveTransitionForTask(Task tsk)
        {
            TaskShell ts;
            if (tsk.IsMultiInstance)
            {
                ts = new MultiTaskShell();
            }
            else
            {
                ts = new TaskShell();
            }
            ts.TaskId = tsk.Id;
            ts.CorrelationId = GetNextTransitionId();
            ts.SetProcessInstance(this);
            ts.ParentCallback = this;
            ts.EnvironmentContext = this.Environment;
            return ts;
        }

        /// <summary>
        /// Check if transition has enough input tokens to be enabled.
        /// Warning: doesn't check if the transition has already been enabled.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="enablingPlaces"></param>
        /// <returns></returns>
        private bool CanEnableTransition(string taskId, out List<string> enablingPlaces)
        {
            enablingPlaces = new List<string>();
            Task tsk = Definition.GetTask(taskId);
            if (tsk.JoinType == JoinType.AND)
            {
                foreach (Place pl in tsk.NodesIn)
                {
                    if (GetNumFreeTokens(pl.Id) > 0)
                    {
                        enablingPlaces.Add(pl.Id);
                    }
                    else return false;
                }
                return true;
            }
            else if (tsk.JoinType == JoinType.XOR)
            {
                foreach (Place pl in tsk.NodesIn)
                {
                    if (GetNumFreeTokens(pl.Id) > 0)
                    {
                        enablingPlaces.Add(pl.Id);
                        return true;
                    }
                }
                return false;
            }
            else if (tsk.JoinType == JoinType.OR)
            {
                foreach (Place pl in tsk.NodesIn)
                {
                    if (GetNumFreeTokens(pl.Id) > 0)
                    {
                        enablingPlaces.Add(pl.Id);
                    }
                }
                if (enablingPlaces.Count == 0)
                {
                    return false; //no input tokens for OR join
                }
                ///now check the OrJoinCheckList. If there are tokens in places from the list,
                ///don't enable the transition - we have to wait until all the tokens disappear from 
                ///these places.
                foreach (string plid in tsk.ORJoinChecklist)
                {
                    Place pl = Definition.GetPlace(plid);
                    if (tsk.NodesIn.Contains(pl)) continue;
                    if (GetTotalTokens(plid) > 0)
                    {
                        log.Info("OR join not enabled: token in {0}", plid);
                        return false;
                    }
                }
                return true;
            }
            else throw new Exception();
        }

        private bool CanEnableTransition(string taskId)
        {
            List<string> enablingPlaces;
            return CanEnableTransition(taskId, out enablingPlaces);
        }

        

        /// <summary>
        /// Handle 'transition selected' event. In this case, all shared 
        /// transitions are cancelled and only the selected transition
        /// remains. 
        /// </summary>
        /// <param name="correlationId"></param>
        private void AfterTransitionSelected(string correlationId)
        {
            TaskShell at = GetActiveTransition(correlationId);
            if (at == null) throw new Exception("Invalid correlation Id");
            log.Info("Transition selected: {0}", at.ToString());
            if (at.Status != TransitionStatus.ENABLED) throw new Exception("Invalid transition status");
            Task tsk = Definition.GetTask(at.TaskId);
            List<string> enablingPlaces;
            bool b = CanEnableTransition(at.TaskId, out enablingPlaces);
            if (!b) throw new Exception("Should never happen!");
            foreach (string plid in enablingPlaces)
            {
                ConsumeToken(plid, correlationId);
            }
            at.SetAllocatedPlaces(enablingPlaces);
            at.Status = TransitionStatus.STARTED;
        }

        /// <summary>
        /// Consume a token from specified place.
        /// Cancels all other transitions if they no longer can be enabled.
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="correlationId"></param>
        private void ConsumeToken(string placeId, string consumingCorrelationId)
        {
            log.Info("ConsumeToken: removing token from {0}", placeId);
            RemoveToken(placeId);
            Place pl = Definition.GetPlace(placeId);
            foreach (Task tsk in pl.NodesOut)
            {
                TaskShell at = GetActiveInstanceOfTask(tsk.Id);
                if (at == null)
                    continue;
                if (at.CorrelationId == consumingCorrelationId)
                    continue;
                if (at.Status == TransitionStatus.STARTED)
                    continue;
                Debug.Assert(at.Status == TransitionStatus.ENABLED);
                bool b = CanEnableTransition(tsk.Id);
                if (!b)
                {
                    log.Info("Transition {0} no longer can be enabled. Cancelling.", at.ToString());
                    CancelTransition(at.CorrelationId);
                }
                else
                    log.Info("Transition {0} still can be enabled", at.ToString());
            }
        }


        /// <summary>
        /// Remove all tokens from a place. Cancel all transitions that 
        /// no longer can be enabled.
        /// </summary>
        /// <param name="placeId"></param>
        private void RemoveAllTokensInPlace(string placeId)
        {
            log.Info("Removing all tokens from {0}", placeId);
            Place pl = Definition.GetPlace(placeId);
            foreach (Task tsk in pl.NodesOut)
            {
                TaskShell ts = GetActiveInstanceOfTask(tsk.Id);
                if (ts != null)
                {
                    if (ts.Status == TransitionStatus.STARTED)
                    {
                        CancelTransition(ts.CorrelationId);
                    }
                }
            }
            while (GetNumFreeTokens(placeId) > 0)
            {
                RemoveToken(placeId);
            }
            foreach (Task tsk in pl.NodesOut)
            {
                TaskShell ts = GetActiveInstanceOfTask(tsk.Id);
                if (ts != null)
                {
                    Debug.Assert(ts.Status == TransitionStatus.ENABLED);
                    CancelTransition(ts.CorrelationId);
                }
            }
            Debug.Assert(GetNumFreeTokens(placeId) == 0);
        }


        /// <summary>
        /// Transition completed - consume input tokens, produce output tokens
        /// and cancel all transitions that share the same tokens.
        /// Also, if some tokens were waiting, put them in 'READY' state.
        /// Wow, looks quite complex.
        /// TODO: fix case when transition completion removes token from 
        /// some or-join checklist
        /// </summary>
        /// <param name="at"></param>
        private void AfterTransitionCompleted(string correlationId)
        {
            lock (this)
            {
                TaskShell at = GetActiveTransition(correlationId);
                if (at == null) throw new Exception("Invalid correlation Id");
                log.Info("Transition completed: {0}", at);
                if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
                Task tsk = Definition.GetTask(at.TaskId);
                if (at.Status == TransitionStatus.ENABLED)
                {
                    AfterTransitionSelected(correlationId);
                }
                Debug.Assert(at.Status == TransitionStatus.STARTED);

                //1. transfer task output data from transition
                at.TransferTaskOutputDataToParent(GetProcessVariablesContainer());
                ValidateProcessInternalData();
                at.Status = TransitionStatus.COMPLETED;

                //handle cancel set
                if (tsk.CancelSet.Count > 0)
                {
                    log.Info("Handling cancel set of task {0}", tsk.Id);
                    foreach (string plid in tsk.CancelSet)
                    {
                        RemoveAllTokensInPlace(plid);
                    }
                }
                
                //and produce output tokens
                int cnt = 0;
                if (tsk.SplitType == JoinType.AND)
                {
                    foreach (Flow fl in tsk.FlowsOut)
                    {
                        if (fl.InputCondition != null && fl.InputCondition.Length > 0) throw new Exception();
                        AddToken(fl.To.Id);
                        cnt++;
                    }
                }
                else if (tsk.SplitType == JoinType.XOR)
                {
                    IList<Flow> flows = tsk.FlowsOutOrdered;
                    for (int i = 0; i < flows.Count; i++)
                    {
                        if (i == flows.Count - 1) 
                        {
                            //last flow - the default one. Always add a token if we are here
                            AddToken(flows[i].To.Id);
                            cnt++;
                        }
                        else
                        {
                            if (EvaluateFlowInputCondition(flows[i]))
                            {
                                AddToken(flows[i].To.Id);
                                cnt++;
                                break;
                            }
                        }
                    }
                }
                else if (tsk.SplitType == JoinType.OR)
                {
                    IList<Flow> flows = tsk.FlowsOutOrdered;
                    for (int i = 0; i < flows.Count; i++)
                    {
                        if (EvaluateFlowInputCondition(flows[i]))
                        {
                            AddToken(flows[i].To.Id);
                            cnt++;
                        }
                    }
                    if (cnt == 0)
                    {
                        //we haven't created any tokens, created the default one
                        AddToken(flows[flows.Count - 1].To.Id);
                        cnt++;
                    }
                }
                else throw new Exception();
                if (cnt == 0) throw new Exception("Transition completion did not produce any tokens");


                //5 notify others
                ActiveTransitionCompleted compl = new ActiveTransitionCompleted();
                compl.CorrelationId = at.CorrelationId;
                compl.InstanceId = this.InstanceId;
                compl.TaskId = at.TaskId;
                compl.TaskType = tsk.GetType().Name;
                compl.TimeStamp = DateTime.Now;
                compl.DefinitionId = this.ProcessDefinitionId;
                NotifyProcessEvent(compl);
            }
        }

        
        /// <summary>
        /// Check flow input condition
        /// </summary>
        /// <param name="fl"></param>
        /// <returns></returns>
        private bool EvaluateFlowInputCondition(Flow fl)
        {
            if (fl.InputCondition == null || fl.InputCondition.Length == 0) return true; //empty condition is true
            IProcessScript ctx = CreateProcessScriptContext();
            object v = ctx.EvaluateFlowInputCondition(fl);
            return Convert.ToBoolean(v);
        }

        

        /// <summary>
        /// Return active transition with given correlation Id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        internal TaskShell GetActiveTransition(string correlationId)
        {
            TaskShell at = null;
            _activeTransitions.TryGetValue(correlationId, out at);
            return at;
        }

        private TaskShell GetActiveInstanceOfTask(string taskDefId)
        {
            foreach (TaskShell ts in _activeTransitions.Values)
            {
                if (ts.TaskId == taskDefId && (ts.Status == TransitionStatus.ENABLED || ts.Status == TransitionStatus.STARTED))
                    return ts;
            }
            return null;
        }


        public IDataObject GetTaskData(string correlationId)
        {
            TaskShell at = GetActiveTransition(correlationId);
            if (at == null)
            {
                MultiTaskShell mti = FindMultiInstanceTransitionWithSubtask(correlationId);
                //if (mti != null) at = mti.GetChildTransition(correlationId);
                
                throw new NotImplementedException();
            }
            return at.GetTaskData();
        }

 
        private string ToXmlString()
        {
            DataObject dob = SaveState();
            return dob.ToXmlString("Process");
        }
        

        public override string ToString()
        {
            return ToXmlString();
        }

        /// <summary>
        /// Retrieve process instance id from task correlation Id.
        /// Task correlation id format is "[process instance id].[task id]"
        /// </summary>
        /// <param name="taskCorrelationId"></param>
        /// <returns></returns>
        public static string ProcessInstanceIdFromTaskCorrelationId(string taskCorrelationId)
        {
            int idx = taskCorrelationId.IndexOf('.');
            if (idx < 0) throw new ArgumentException("Invalid correlation id");
            return taskCorrelationId.Substring(0, idx);
        }

        /// <summary>
        /// Find multi-instance transition containing subtask with specified correlation Id
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private MultiTaskShell FindMultiInstanceTransitionWithSubtask(string correlationId)
        {
            IList<TaskShell> lst = GetActiveTransitions();
            foreach (TaskShell at in lst)
            {
                if (at is MultiTaskShell)
                {
                    MultiTaskShell mti = (MultiTaskShell)at;
                    if (mti.HasSubTask(correlationId)) return mti;
                }
            }
            return null;
        }

        /// <summary>
        /// Dispatch internal transition event to proper 
        /// ActiveTransition object
        /// </summary>
        /// <param name="ite"></param>
        internal virtual bool DispatchInternalTransitionEvent(InternalTransitionEvent ite)
        {
            if (Status != ProcessStatus.Ready &&
                Status != ProcessStatus.Waiting)
            {
                log.Info("Process {0} - ignoring transition event {1} because process is finished or cancelled", InstanceId, ite);
                return false;
            }
            if (!_activated) throw new Exception("Process instance not activated");
            if (ite.ProcessInstanceId != this.InstanceId) throw new ApplicationException("Incorrect activation id");
            TaskShell at = GetActiveTransition(ite.CorrelationId);
            if (at == null)
            {
                at = FindMultiInstanceTransitionWithSubtask(ite.CorrelationId);
            }
            if (at == null)
            {
                log.Warn("Internal transition event: did not find transition {0}", ite.CorrelationId);
                return false;
            }
            return at.HandleInternalTransitionEvent(ite);
        }

        

        



        #region ITransitionCallback Members

        /// <summary>
        /// Handle callback from task shell that the transition has been started.
        /// </summary>
        /// <param name="correlationId"></param>
        void IProcessTransitionCallback.TransitionStarted(string correlationId)
        {
            this.AfterTransitionSelected(correlationId);
        }

        /// <summary>
        /// Handle callback from task shell that the transition has completed.
        /// </summary>
        /// <param name="correlationId"></param>
        void IProcessTransitionCallback.TransitionCompleted(string correlationId)
        {
            this.AfterTransitionCompleted(correlationId);
        }

        #endregion




        /// <summary>
        /// Cancel active transition. 
        /// Returns tokens to input places if the transition has been STARTED.
        /// </summary>
        /// <param name="correlationId"></param>
        private void CancelTransition(string correlationId)
        {
            TaskShell at = GetActiveTransition(correlationId);
            if (at == null) throw new Exception("Invalid correlation Id");
            log.Info("Cancelling transition {0}", at.ToString());
            if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
            if (at.Status == TransitionStatus.STARTED)
            {
                //return tokens to input places
                foreach (string plid in at.AllocatedPlaces)
                {
                    AddToken(plid);
                }
            }
            at.CancelTask();
            at.Status = TransitionStatus.CANCELLED;

            ActiveTransitionCancelled ac = new ActiveTransitionCancelled();
            ac.CorrelationId = at.CorrelationId;
            ac.DefinitionId = ProcessDefinitionId;
            ac.InstanceId = InstanceId;
            ac.TaskId = at.TaskId;
            ac.TaskType = Definition.GetTask(at.TaskId).GetType().Name;
            NotifyProcessEvent(ac);
        }

        /// <summary>
        /// Cancel process instance
        /// Cancels all currently active transitions and removes all tokens
        /// from the process.
        /// TODO: implement
        /// </summary>
        public void CancelProcessInstance()
        {
            log.Info("Cancelling process");
            lock (this)
            {
                foreach (string corrId in _activeTransitions.Keys)
                {
                    TaskShell ts = GetActiveTransition(corrId);
                    if (ts.Status == TransitionStatus.ENABLED ||
                        ts.Status == TransitionStatus.STARTED)
                    {
                        CancelTransition(corrId);
                    }
                }
                _currentMarking = new Dictionary<string, int>();
                _status = ProcessStatus.Cancelled;
            }
            log.Info("Process cancelled");
            ProcessCancelled pc = new ProcessCancelled();
            pc.CorrelationId = this.CorrelationId;
            pc.DefinitionId = this.ProcessDefinitionId;
            pc.InstanceId = this.InstanceId;
            NotifyProcessEvent(pc);
        }

        public static readonly string APIVERSION = "1.0";


        /// <summary>
        /// Save process state in a DataObject
        /// This is a custom persistence mechanism, alternative to .Net binary serialization.
        /// Data can be persisted as XML, therefore can be externally read and modified. Also,
        /// version update in production environment should be easier to perform if process
        /// instances are stored in a readable format.
        /// </summary>
        /// <returns></returns>
        public DataObject SaveState()
        {
            DataObject dob = new DataObject();
            dob["APIVersion"] = APIVERSION;
            dob["InstanceId"] = this.InstanceId;
            dob["ProcessDefinitionId"] = this.ProcessDefinitionId;
            dob["Status"] = this.Status.ToString();
            dob["PersistedVersion"] = this._persistedVersion;
            dob["InstanceData"] = new DataObject(this.GetProcessVariablesContainer());
            dob["CorrelationId"] = this._correlationId == null ? "" : this.CorrelationId;
            dob["TransitionNumber"] = this._transitionNumber;
            dob["StartedBy"] = this._startedBy;
            dob["StartDate"] = this.StartDate.ToString();
            List<object> al = new List<object>();
            foreach (string plid in _currentMarking.Keys)
            {
                int n = _currentMarking[plid];
                if (n > 0)
                {
                    DataObject d2 = new DataObject();
                    d2["Place"] = plid;
                    d2["Tokens"] = n.ToString();
                    al.Add(d2);
                }
            }
            dob["Marking"] = al;
            List<object> al2 = new List<object>();
            foreach (TaskShell ts in this._activeTransitions.Values)
            {
                al2.Add(ts.SaveState());
            }
            dob["Transition"] = al2;
            return dob;
        }

        /// <summary>
        /// Restore process state from a DataObject (obtained by calling SaveProcessState).
        /// This is the preferred way of storing process state in NGinn. Of course
        /// process instance can be binary serialized and saved, but the binary format
        /// causes problems when upgrading class versions.
        /// </summary>
        /// <param name="dob"></param>
        public void RestoreState(DataObject dob)
        {
            if (this._activated) throw new ApplicationException("Restore is not possible after activation");
            string v = (string) dob["APIVersion"];
            if (!APIVERSION.Equals(v))
            {
                log.Warn("Trying to restore process state from version {0}. API version is {1}", v, APIVERSION);
            }
            _instId = (string) dob["InstanceId"];
            _definitionId = (string) dob["ProcessDefinitionId"];
            _status = (ProcessStatus)Enum.Parse(typeof(ProcessStatus), (string) dob["Status"]);
            _persistedVersion = Convert.ToInt32(dob["PersistedVersion"]);
            _startedBy = (string)dob["StartedBy"];
            v = (string) dob["StartDate"];
            if (v != null) _startDate = DateTime.Parse(v);
            DataObject vars = (DataObject)dob["InstanceData"];
            _processInstanceData = new DataObject();
            _processInstanceData["variables"] = vars;
            _correlationId = (string)dob["CorrelationId"];
            _transitionNumber = Convert.ToInt32(dob["TransitionNumber"]);
            _currentMarking = new Dictionary<string, int>();
            IList l = dob.GetArray("Marking");
            if (l != null)
            {
                foreach (DataObject d2 in l)
                {
                    string plid = (string)d2["Place"];
                    int n = Convert.ToInt32(d2["Tokens"]);
                    _currentMarking[plid] = n;
                }
            }
            _activeTransitions = new Dictionary<string, TaskShell>();
            IList l2 = dob.GetArray("Transition");
            if (l2 != null)
            {
                foreach (DataObject dob2 in l2)
                {
                    TaskShell ts = TaskShell.RestoreTaskShell(dob2);
                    _activeTransitions[ts.CorrelationId] = ts;
                }
            }
        }
    }
}

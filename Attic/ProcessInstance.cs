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
using ScriptNET;
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
        /// <summary>helper dictionary for quick lookup of tokens in place</summary>
        [NonSerialized]
        private IDictionary<string, IList<Token>> _tokensInPlaces = new Dictionary<string, IList<Token>>();
        /// <summary>tokens in process: token id->token </summary>
        private IDictionary<string, Token> _tokens = new Dictionary<string, Token>();
        
        private int _persistedVersion;
        [NonSerialized]
        private bool _activated = false;
        [NonSerialized]
        private INGEnvironmentContext _environment;
        /// <summary>map: correlation id->transition</summary>
        private IDictionary<string, TaskShell> _activeTransitions = new Dictionary<string, TaskShell>();
        /// <summary>helper map: task id -> list of active instances of the task</summary>
        [NonSerialized]
        private IDictionary<string, IList<TaskShell>> _activeTaskTransitions = new Dictionary<string, IList<TaskShell>>();
        private ProcessStatus _status;
        private int _tokenNumber = 0;
        private int _transitionNumber = 0;
        private DataObject _processInstanceData = new DataObject();
        
        private string _correlationId;

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

        

        /// <summary>
        /// Update _tokensInPlaces dictionary
        /// </summary>
        private void BuildTokensInPlaces()
        {
            Dictionary<string, IList<Token>> newTokens = new Dictionary<string, IList<Token>>();
            foreach (Token t in _tokens.Values)
            {
                if (Definition.GetPlace(t.PlaceId) == null)
                    throw new Exception("Invalid token place: " + t.PlaceId);
                IList<Token> lst;
                if (!newTokens.TryGetValue(t.PlaceId, out lst))
                {
                    lst = new List<Token>();
                    newTokens[t.PlaceId] = lst;
                }
                lst.Add(t);
            }
            _tokensInPlaces = newTokens;
        }

        private void BuildActiveTransitionsInTasks()
        {
            Dictionary<string, IList<TaskShell>> newTransitions = new Dictionary<string, IList<TaskShell>>();
            foreach (TaskShell at in this._activeTransitions.Values)
            {
                IList<TaskShell> ats;
                if (!newTransitions.TryGetValue(at.TaskId, out ats))
                {
                    ats = new List<TaskShell>();
                    newTransitions[at.TaskId] = ats;
                }
                ats.Add(at);
                
            }
            _activeTaskTransitions = newTransitions;
        }
        

        /// <summary>
        /// Get all tokens in specified place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public IList<Token> GetTokensInPlace(string placeId)
        {
            if (_tokensInPlaces.ContainsKey(placeId))
                return _tokensInPlaces[placeId];
            return new List<Token>();
        }

        /// <summary>
        /// Get all process instance tokens
        /// </summary>
        /// <returns></returns>
        public IList<Token> GetAllTokens()
        {
            return new List<Token>(_tokens.Values);
        }

        /// <summary>
        /// Returns all active tokens in process
        /// </summary>
        /// <returns></returns>
        public IList<Token> GetActiveProcessTokens()
        {
            List<Token> l = new List<Token>();
            foreach (Token t in _tokens.Values)
            {
                if (t.Status == TokenStatus.READY ||
                    t.Status == TokenStatus.WAITING ||
                    t.Status == TokenStatus.LOCKED_ALLOCATED ||
                    t.Status == TokenStatus.LOCKED_ENABLED)
                    l.Add(t);
            }
            return l;
        }

        /// <summary>
        /// Allocate next token identifier
        /// </summary>
        /// <returns></returns>
        private string GetNextTokenId()
        {
            int n;
            lock (this)
            {
                n = _tokenNumber;
                _tokenNumber++;
            }
            return string.Format("{0}.{1}", _instId, n);
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
        /// Create new token in start place. Used for starting new process instance.
        /// </summary>
        /// <returns></returns>
        public Token CreateNewStartToken()
        {
            return CreateNewTokenInPlace(Definition.Start.Id);
        }

        /// <summary>
        /// Create new token in given place.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Token CreateNewTokenInPlace(string placeId)
        {
            if (!_activated) throw new Exception("Process instance not activated");
            Token t = new Token();
            t.TokenId = GetNextTokenId();
            t.PlaceId = this.Definition.GetPlace(placeId).Id;
            t.Status = TokenStatus.READY;
            return t;
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
        public void AddToken(Token tok)
        {
            if (!_activated) throw new Exception("Process instance not activated");
            lock (this)
            {
                if (_tokens.ContainsKey(tok.TokenId)) throw new Exception("Token already exists");
                if (_definition.GetPlace(tok.PlaceId) == null) throw new Exception("Invalid token place id");
                Place pl = Definition.GetPlace(tok.PlaceId);
                if (pl == null) throw new Exception("Invalid token place id");
                _tokens[tok.TokenId] = tok;
                IList<Token> lst;
                if (!_tokensInPlaces.TryGetValue(tok.PlaceId, out lst))
                {
                    lst = new List<Token>();
                    _tokensInPlaces[tok.PlaceId] = lst;
                }
                lst.Add(tok);
                _status = ProcessStatus.Ready;
                if (pl == Definition.Start && _tokens.Count == 1)
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
        protected internal IScriptContext CreateProcessScriptContext()
        {
            IScriptContext ctx = new ScriptContext();
            DataObject env = new DataObject(Environment.EnvironmentVariables);
            env["log"] = log;
            env["messageBus"] = Environment.MessageBus;
            env["environment"] = Environment;
            env["processDefinition"] = Definition;
            ctx.SetItem("__env", ContextItem.Variable, env);
            
            IDataObject variables = GetProcessDataSource();
            if (variables != null)
            {
                foreach (string fn in variables.FieldNames)
                {
                    ctx.SetItem(fn, ContextItem.Variable, variables[fn]);
                }
            }
            return ctx;
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
            
            IScriptContext ctx = CreateProcessScriptContext();
            ctx.SetItem("data", ContextItem.Variable, new DOBMutant(dob));
            
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
                        object val = Script.RunCode(vd.DefaultValueExpr, ctx);
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
        /// Return token with given Id
        /// </summary>
        /// <param name="tokenId"></param>
        /// <returns></returns>
        public Token GetToken(string tokenId)
        {
            Token tok;
            if (_tokens.TryGetValue(tokenId, out tok)) return tok;
            return null;
        }
        

        /// <summary>
        /// executes one or more process steps
        /// returns true - if process could continue
        /// returns false - if process cannot continue
        /// </summary>
        /// <returns></returns>
        public bool Kick()
        {
            Token tok = SelectReadyTokenForProcessing();
            if (tok == null)
            {
                Debug.Assert(Status != ProcessStatus.Ready);
                return false;
            }
            KickToken(tok.TokenId);
            return Status == ProcessStatus.Ready;
        }

        /// <summary>
        /// Passivate is called before persisting the process instance data
        /// </summary>
        public void Passivate()
        {
            log.Info("Passivating");
            _tokensInPlaces = null;
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
            
            _definition = Environment.DefinitionRepository.GetProcessDefinition(ProcessDefinitionId);
            BuildTokensInPlaces();
            BuildActiveTransitionsInTasks();
            foreach (TaskShell at in _activeTransitions.Values)
            {
                at.SetProcessInstance(this);
                at.Activate();
            }
            _activated = true;
        }

        /// <summary>
        /// Choose ready token that will be processed next.
        /// </summary>
        /// <returns></returns>
        public Token SelectReadyTokenForProcessing()
        {
            if (!_activated) throw new Exception("Process instance not activated");
            lock (this)
            {
                foreach (Token tok in _tokens.Values)
                {
                    if (tok.Status == TokenStatus.READY)
                    {
                        return tok;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Return a list of active transitions for given task
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        private IList<TaskShell> GetActiveInstancesOfTask(string taskId)
        {
            IList<TaskShell> lst;
            if (_activeTaskTransitions.TryGetValue(taskId, out lst))
                return lst;
            return new List<TaskShell>();
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
        /// Invoked when a token has reached process end place
        /// </summary>
        /// <param name="tok"></param>
        private void OnEndPlaceReached(Token tok)
        {
            Place pl = Definition.GetPlace(tok.PlaceId);
            Debug.Assert(pl == Definition.Finish);
            log.Info("Token {0} has reached process end", tok.ToString());
            tok.Status = TokenStatus.CONSUMED;
            bool finished = true;
            IList<Token> toks = GetActiveProcessTokens();
            if (toks.Count > 0)
            {
                log.Info("Process cannot finish: {0} tokens still alive", toks.Count);
                return;
            }
            log.Info("No more tokens alive - process has finished");
            OnProcessFinished();
        }


        /// <summary>
        /// Invoked when process has finished
        /// </summary>
        private void OnProcessFinished()
        {
            Debug.Assert(GetActiveProcessTokens().Count == 0);
            _status = ProcessStatus.Finished;
            ProcessFinished pf = new ProcessFinished();
            pf.InstanceId = InstanceId;
            pf.DefinitionId = ProcessDefinitionId;
            pf.CorrelationId = CorrelationId;
            NotifyProcessEvent(pf);
        }

        public void KickToken(string tokenId)
        {
            try
            {
                Token tok = GetToken(tokenId);
                KickReadyToken(tok);
                Token t2 = SelectReadyTokenForProcessing();
                if (_status == ProcessStatus.Ready || _status == ProcessStatus.Waiting)
                {
                    _status = t2 != null ? ProcessStatus.Ready : ProcessStatus.Waiting;
                }
            }
            catch (Exception ex)
            {
                log.Error("Error: {0}", ex);
                throw;
            }
        }
        /// <summary>
        /// Kick a 'READY' token. It means-> initiate all tasks following current place - only if the tasks can be initiated.
        /// </summary>
        /// <param name="tok"></param>
        protected void KickReadyToken(Token tok)
        {
            if (!_activated) throw new Exception("Process instance not activated");
            log.Info("Kicking token {0}", tok.ToString());
            if (_tokens[tok.TokenId] != tok) throw new Exception("invalid token");
            if (tok.Status != TokenStatus.READY) throw new Exception("Token status is not Ready - cannot kick");
            
            Place pl = Definition.GetPlace(tok.PlaceId);
            if (pl is EndPlace)
            {
                Debug.Assert(pl == Definition.Finish);
                OnEndPlaceReached(tok);
                return;
            }
            IList<Token> toks = GetConsumableTokensInPlace(tok.PlaceId);
            //Strategy 1: if there are enabled transition (that is, tokens in WAITING_TASK state), 
            //do nothing and put the token in WAITING state. So actually we will be waiting for the transition
            //to fire before enabling other transitions
            foreach (Token tok1 in toks)
            {
#warning "Do we still use this strategy? Why checking only LOCKED_ENABLED - what about LOCKED_ALLOCATED?"
                if (tok1.Status == TokenStatus.LOCKED_ENABLED)
                {
                    log.Info("There are active transitions in this place. Putting token in WAITING state");
                    tok.Status = TokenStatus.WAITING;
                    return;
                }
            }
            //Strategy 2: it's up to system to decide which tokens are selected when the transition fires.
            //so any combination of tokens that enables the transition is good.
            //However, each enabled transition results in a task being created, and we don't want to create
            //a task for each combination of tokens for the transition. Therefore we will select only these
            //tokens that were not used for enabling the same transition
            //With this algorithm, we create separate groups of tokens that enable particular transition,
            //and we enable the transition as many times as there are unallocated tokens left.

            //procedura enablowania tranzycji
            //jesli mamy 1 token, to nie moze byc enabled transition
            //jesli mamy > 1 token, to niektore moga byc enabled
            //jesli mamy 1 wyjscie, to nie ma problemu
            //jesli mamy > 1 wyjscie, to trzeba to jakos podzielic
            //najwazniejsze ze tranzycja nie rozroznia tokenow!!! (nie ma input condition) - sztuka to sztuka
            //jak dojdzie nowy token, to na pewno nie odpala tranzycje inne niz te co juz sa
            //tak, o ile mamy symetrie
            //Na razie zrobmy tak, ze o ile transition jest enabled to nie mozna juz enablowac innych dopoki poprzednia
            //nie odpali
            //wersja pe�na
            //wchodzi token. Dla kazdego wychodzacego tasku: 
            //  nEn = ile task ma enabled tranzycji. 
            //  sprawdz, czy task mozna enablowac, ale przy zalozeniu ze nie bierzemy tokenow z tranzycji dla tego tasku 
            //  ktore juz sa enablowane
            IList<TaskShell> newAts = new List<TaskShell>();

            foreach (Task tsk in pl.NodesOut)
            {
                //1. calculating set of tokens that were used for enabling tsk 
                //   so we don't use them for enabling next instance of it
                IDictionary<string, string> tokDict = new Dictionary<string, string>();
                IList<TaskShell> taskTransitions = GetActiveInstancesOfTask(tsk.Id);
                foreach (TaskShell at in taskTransitions)
                {
                    Debug.Assert(at.Status == TransitionStatus.ENABLED || at.Status == TransitionStatus.STARTED);
                    Debug.Assert(at.Tokens.Count > 0);
                    foreach (string tid in at.Tokens)
                    {
                        if (!tokDict.ContainsKey(tid)) tokDict[tid] = tid;
                    }
                }

                IList<Token> enablingTokens = null;
                //ok, now let's check if transition can be enabled without using tokens from the tokDict
                bool b = CanEnableTransition(tsk, tokDict, out enablingTokens);
                log.Info("Checking if transition {0} can be initiated: {1}", tsk.Id, b);
                if (b)
                {
#warning "we have a problem with immediate tasks here..."
                    //create transition instance and assign tokens to it
                    TaskShell at = CreateActiveTransitionForTask(tsk);
                    foreach (Token t in enablingTokens)
                    {
                        at.Tokens.Add(t.TokenId);
                    }

                    newAts.Add(at);
                    /* old code for dealing with immediate tasks
                    IActiveTask atsk = Environment.ActiveTaskFactory.CreateActiveTask(tsk);

                    if (atsk.IsImmediate)
                    {
                        //if task is immediate, no other tasks will run so we clear the newAts list
                        log.Info("Task {0} is immediate. Will start it now.", tsk.Id);
                        newAts.Clear();
                        newAts.Add(at);
                        break; //leave the loop
                    }
                    else
                    {
                        newAts.Add(at);
                    }*/
                }
            }

            if (newAts.Count == 0)
            {
                log.Info("No new transitions can be initiated. Marking token {0} as WAITING", tok.ToString());
#warning "Can we be sure that it is the token to be marked WAITING?... All tokens are equal, ok, but..."
                Debug.Assert(tok.Status == TokenStatus.READY);
                tok.Status = TokenStatus.WAITING;
                return;
            }

            //ok, now start the transitions
            //
            //but before starting, find all transitions that share tokens and 'link' them together
            foreach (TaskShell at in newAts)
            {
#warning "Beware of race condition here. Initiated task can start or complete before we actually initiate all other tasks"
                at.Activate();
                foreach (string tokId in at.Tokens)
                {
                    Token t = GetToken(tokId);
                    if (t.Status == TokenStatus.CANCELLED ||
                        t.Status == TokenStatus.CONSUMED)
                    {
                        Debug.Assert(false, "Dead token");
                        throw new Exception("Cannot activate transition because it has a dead input token");
                    }
                    else if (t.Status == TokenStatus.LOCKED_ALLOCATED)
                    {
                        Debug.Assert(false, "Allocated token");
                        throw new Exception("Cannot activate transition because it has an already allocated input token");
                    }
                }

                AddActiveTransition(at);
                foreach (string tokId in at.Tokens)
                {
                    Token tok1 = GetToken(tokId);
                    tok1.Status = TokenStatus.LOCKED_ENABLED;
                    log.Info("Changed status of token ({0}) to {1}", tok1.TokenId, tok1.Status);
                }
                InitiateTransition(at);
                log.Info("Initiated transition {0}: {1}->{2}", at.CorrelationId, tok.PlaceId, at.TaskId);
            }

            
        }

        /// <summary>
        /// Transition can be initiated if it has required number (depending on join type) of tokens in statuses:
        /// READY, WAITING or WAITING_TASK
        /// </summary>
        /// <param name="tsk"></param>
        /// <returns></returns>
        private bool CanEnableTransition(Task tsk, IDictionary<string, string> ignoreTokens, out IList<Token> enablingTokens)
        {
            enablingTokens = new List<Token>();
            if (tsk.JoinType == JoinType.AND)
            {
                foreach (Place pl in tsk.NodesIn)
                {
                    bool foundTokInPlace = false;
                    IList<Token> toksInPlace = GetConsumableTokensInPlace(pl.Id);
                    foreach (Token t in toksInPlace)
                    {
                        if (!ignoreTokens.ContainsKey(t.TokenId))
                        {
                            foundTokInPlace = true;
                            enablingTokens.Add(t);
                            break;
                        }
                    }
                    if (!foundTokInPlace)
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (tsk.JoinType == JoinType.XOR)
            {
                foreach (Place pl in tsk.NodesIn)
                {
                    IList<Token> toksInPlace = GetConsumableTokensInPlace(pl.Id);
                    foreach (Token t in toksInPlace)
                    {
                        if (!ignoreTokens.ContainsKey(t.TokenId))
                        {
                            enablingTokens.Add(t);
                            return true;
                        }
                    }
                }
                return false;
            }
            else if (tsk.JoinType == JoinType.OR)
            {
                foreach (Place pl in tsk.NodesIn)
                {
                    bool foundTokInPlace = false;
                    IList<Token> toksInPlace = GetConsumableTokensInPlace(pl.Id);
                    foreach (Token t in toksInPlace)
                    {
                        if (!ignoreTokens.ContainsKey(t.TokenId))
                        {
                            foundTokInPlace = true;
                            enablingTokens.Add(t);
                            break;
                        }
                    }
                }
                if (enablingTokens.Count == 0)
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
                    IList<Token> toks = GetTokensInPlace(plid);
                    foreach (Token t in toks)
                    {
                        if (t.Status == TokenStatus.READY ||
                            t.Status == TokenStatus.WAITING ||
                            t.Status == TokenStatus.LOCKED_ALLOCATED ||
                            t.Status == TokenStatus.LOCKED_ENABLED)
                        {
                            log.Info("OR join not enabled: token in {0}", plid);
                            return false;
                        }
                    }
                }
                return true;
            }
            else throw new Exception();
        }

        /// <summary>
        /// List of 'consumable' tokens, that is tokens suitable for being consumed by a transition,
        /// or for initiating a transition. It consists of READY, WAITING and LOCKED_ENABLED tokens.
        /// After initiating a transition, they become LOCKED_ALLOCATED.
        /// If the transition is immediate, it will execute and consume the tokens.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        private IList<Token> GetConsumableTokensInPlace(string placeId)
        {
            List<Token> lst = new List<Token>();
            List<Token> l2 = new List<Token>();
            IList<Token> toks = GetTokensInPlace(placeId);
            foreach (Token t in toks)
            {
                if (t.Status == TokenStatus.READY || t.Status == TokenStatus.WAITING)
                    lst.Add(t);
                else if (t.Status == TokenStatus.LOCKED_ENABLED)
                    l2.Add(t);
            }
            lst.AddRange(l2);
            return lst;
        }

        

        /// <summary>
        /// Initiate task
        /// </summary>
        /// <param name="at"></param>
        private void InitiateTransition(TaskShell at)
        {
            log.Info("Initiating transition {0}", at.TaskId);
            at.InitiateTask(GetProcessDataSource());
            if (at.IsImmediate)
            {
                if (at.Status != TransitionStatus.COMPLETED)
                    throw new Exception("Immediate transition did not complete after initiation");
            }
        }


        
        private TaskShell CreateActiveTransitionForTask(Task tsk)
        {
            if (tsk.IsMultiInstance)
            {
                MultiTaskShell mts = new MultiTaskShell(this, tsk.Id);
                mts.CorrelationId = GetNextTransitionId();
                mts.SetProcessInstance(this);
                mts.ParentCallback = this;
                return mts;
            }
            else
            {

                TaskShell ts = new TaskShell(this, tsk.Id);
                ts.CorrelationId = GetNextTransitionId();
                ts.SetProcessInstance(this);
                ts.ParentCallback = this;
                return ts;
            }
        }

        /// <summary>
        /// Add initialized active transition to the collection of active transitions
        /// Updates transition list for each input token of at. Calculates sharedId for
        /// the transition, if the transition is in implicit-choice group.
        /// TODO: fix the sharedId calculation code (for now fixed by allowing only one shared Id).
        /// </summary>
        /// <param name="at"></param>
        private void AddActiveTransition(TaskShell at)
        {
            if (at.CorrelationId == null) throw new Exception("Correlation id required");
            if (at.Tokens.Count == 0) throw new Exception("Cannot add a transition without tokens");
            Debug.Assert(InstanceId == at.ProcessInstanceId);
            Debug.Assert(Definition.GetTask(at.TaskId) != null);
            //find shared transitions...
            IDictionary<string, TaskShell> sharedTrans = new Dictionary<string, TaskShell>();
            foreach (string tid in at.Tokens)
            {
                Token t = GetToken(tid);
                foreach (string atid in t.ActiveTransitions)
                {
                    Debug.Assert(atid != at.CorrelationId); //at cannot be in token's transitions yet
                    if (!sharedTrans.ContainsKey(atid)) 
                        sharedTrans[atid] = GetActiveTransition(atid);
                }
            }
            List<TaskShell> strans = new List<TaskShell>(sharedTrans.Values);
            if (strans.Count > 0)
            {
                log.Info("Transition shares the same tokens with {0} active transitions.");
                //IMPORTANT: currently we enable implicit choice groups with only one input token
                //so if there are shared transitions and they have more than one input tokens
                //just throw an error. TODO: in future we should handle such situation
                //somehow
                foreach (TaskShell at2 in strans)
                {
                    if (at2.Tokens.Count > 1) throw new ApplicationException(string.Format("Implicit choice groups do not currently allow more than one input token. For transition {0} we have more than 1 input token", at2.CorrelationId));
                }
                if (at.Tokens.Count > 1) throw new ApplicationException(string.Format("Implicit choice groups do not currently allow more than one input token. For transition {0} we have more than 1 input token", at.CorrelationId));
                at.SharedId = at.Tokens[0];
                //end IMPORTANT:
                /* 
                if (strans.Count == 1)
                {
                    strans[0].SharedId = strans[0].CorrelationId;
                }
                at.SharedId = strans[0].SharedId;
                */
                foreach (TaskShell at2 in strans) Debug.Assert(at2.SharedId == at.SharedId);
            }
            else
            {
                at.SharedId = at.Tokens[0];
            }

            Task tsk = Definition.GetTask(at.TaskId);
            _activeTransitions[at.CorrelationId] = at;
            IList<TaskShell> ats;
            if (!_activeTaskTransitions.TryGetValue(at.TaskId, out ats))
            {
                ats = new List<TaskShell>();
                _activeTaskTransitions[at.TaskId] = ats;
            }
            ats.Add(at);
            foreach (string tokid in at.Tokens)
            {
                Token tok = GetToken(tokid);
                tok.ActiveTransitions.Add(at.CorrelationId);
            }
        }

        /// <summary>
        /// Return a list of transitions sharing some tokens with specified transition
        /// </summary>
        /// <param name="at"></param>
        /// <returns></returns>
        private IList<TaskShell> GetSharedActiveTransitionsForTransition(TaskShell at)
        {
            IDictionary<string, TaskShell> dict = new Dictionary<string, TaskShell>();
            foreach (string tokid in at.Tokens)
            {
                Token t = GetToken(tokid);
                foreach (string atid in t.ActiveTransitions)
                {
                    if (dict.ContainsKey(atid)) continue;
                    TaskShell at2 = GetActiveTransition(atid);
                    if (at2 == at) continue;
                    if (at2.Status == TransitionStatus.ENABLED)
                    {
                        dict[atid] = at2;
                    }
                }
            }
            return new List<TaskShell>(dict.Values);
        }

        

        /// <summary>
        /// Handle 'transition selected' event. In this case, all shared 
        /// transitions are cancelled and only the selected transition
        /// remains. Tokens are switched to 'LOCKED_ALLOCATED' status.
        /// </summary>
        /// <param name="correlationId"></param>
        private void AfterTransitionSelected(string correlationId)
        {
            TaskShell at = GetActiveTransition(correlationId);
            if (at == null) throw new Exception("Invalid correlation id");
            
            bool found = false;
            foreach (string t in at.Tokens)
            {
                Token tok = GetToken(t);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ENABLED ||
                    tok.Status == TokenStatus.LOCKED_ALLOCATED);
                if (tok.Status == TokenStatus.LOCKED_ENABLED)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                log.Debug("Did not find any locked_enabled tokens");
                Debug.Assert(GetSharedActiveTransitionsForTransition(at).Count == 0);
                return;
            }

            log.Info("Transition selected: {0}", at.CorrelationId);
            Task tsk = Definition.GetTask(at.TaskId);
            //1 find all 'shared' transitions and cancel them
            IList<TaskShell> sharedTrans = GetSharedActiveTransitionsForTransition(at);
            if (sharedTrans.Count > 0)
            {
                log.Info("Found {0} active transitions to cancel", sharedTrans.Count);
                foreach (TaskShell at2 in sharedTrans)
                {
                    CancelActiveTransition(at2);
                }
            }
            Debug.Assert(GetSharedActiveTransitionsForTransition(at).Count == 0);
            foreach (string tokid in at.Tokens)
            {
                Token tok = GetToken(tokid);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ENABLED);
                Debug.Assert(tok.ActiveTransitions.Count == 1 && tok.ActiveTransitions[0] == at.CorrelationId);
                //if (!at.IsImmediate && tok.Status != TokenStatus.WAITING_ENABLED) throw new Exception();
                tok.Status = TokenStatus.LOCKED_ALLOCATED;
            }
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
            TaskShell at = _activeTransitions[correlationId];
            log.Info("Transition completed: {0}", at.CorrelationId);
            Task tsk = Definition.GetTask(at.TaskId);
            //1 select the transition for processing
            bool found = false;
            foreach (string t in at.Tokens)
            {
                Token tok = GetToken(t);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ENABLED ||
                    tok.Status == TokenStatus.LOCKED_ALLOCATED);
                if (tok.Status == TokenStatus.LOCKED_ENABLED)
                {
                    found = true;
                    break;
                }
            }
            if (found)
            {
                AfterTransitionSelected(correlationId);
            }
            IList<TaskShell> sharedTrans = GetSharedActiveTransitionsForTransition(at);
            Debug.Assert(sharedTrans.Count == 0); //after transition selected there should be no shared trans.
            foreach (string tokid in at.Tokens)
            {
                Token tok = GetToken(tokid);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ALLOCATED);
            }
            
            //at.TaskCompleted();
            //at.Status = TransitionStatus.COMPLETED;
            //2 retrieve data from transition
            //TransferDataFromTransition(at);
            //3 cancel set handling
            if (tsk.CancelSet.Count > 0)
            {
                log.Debug("Transition {0} ({1}) has cancel set with {2} elements", tsk.Id, at.CorrelationId, tsk.CancelSet.Count);
                foreach (string placeId in tsk.CancelSet)
                {
                    IList<Token> tokensInPlace = GetTokensInPlace(placeId);
                    foreach (Token tok in tokensInPlace)
                    {
                        if (tok.Status == TokenStatus.READY ||
                            tok.Status == TokenStatus.LOCKED_ENABLED ||
                            tok.Status == TokenStatus.LOCKED_ALLOCATED ||
                            tok.Status == TokenStatus.WAITING)
                        {
                            log.Debug("Cancelling token {0} in place {1}", tok.TokenId, placeId);
                            CancelToken(tok);
                        }
                    }
                }
            }
            //4 move the tokens
            UpdateNetStatusAfterTransition(at);
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

        

        /// <summary>
        /// Cancellation handling. Removes token from its current place and cancels
        /// all transitions that the token has enabled.
        /// TODO: fix case when cancel removes token from or-join checklist
        /// </summary>
        /// <param name="tok"></param>
        private void CancelToken(Token tok)
        {
            if (tok.Status == TokenStatus.READY ||
                tok.Status == TokenStatus.WAITING)
            {
                Debug.Assert(tok.ActiveTransitions.Count == 0); //ready or waiting token canot have transitions
                tok.Status = TokenStatus.CANCELLED;
            }
            else if (tok.Status == TokenStatus.LOCKED_ALLOCATED ||
                tok.Status == TokenStatus.LOCKED_ENABLED)
            {
                Debug.Assert(tok.ActiveTransitions.Count > 0); //must have at least one transition
                List<string> lst = new List<string>(tok.ActiveTransitions);//clone the list
                foreach (string atId in lst)
                {
                    TaskShell at = GetActiveTransition(atId);
                    Debug.Assert(at.Status == TransitionStatus.ENABLED || at.Status == TransitionStatus.STARTED);
                    log.Debug("Cancelling transition {0}  because token {1} ({2})is cancelled", at.CorrelationId, tok.TokenId, tok.PlaceId);
                    CancelActiveTransition(at);
                    //after transition is cancelled, token should return to 'READY' or 'WAITING' state
                    Debug.Assert(tok.Status == TokenStatus.READY || tok.Status == TokenStatus.WAITING);
                    tok.Status = TokenStatus.CANCELLED;
                }
            }
            else throw new Exception("Invalid token status");
            UpdateOrJoinChecklistStatusAfterTokenRemoval(tok);
        }

        /// <summary>
        /// When token is removed from some place that belongs to or-join's checklist,
        /// it can enable the or-join as no more tokens will be in the checklist.
        /// This method updates the status of or-join's waiting input tokens so next time
        /// nginn will re-evaluate or-join's status.
        /// </summary>
        /// <param name="tok">removed token</param>
        private void UpdateOrJoinChecklistStatusAfterTokenRemoval(Token tok)
        {
            Debug.Assert(tok.Status == TokenStatus.CONSUMED || tok.Status == TokenStatus.CANCELLED);
            IList<Task> lst = Definition.GetOrJoinsWithPlaceInChecklist(tok.PlaceId);
            if (lst.Count > 0) 
            {
                foreach (Task tsk in lst)
                {
                    foreach(Place pl in tsk.NodesIn)
                    {
                        foreach (Token t2 in GetTokensInPlace(pl.Id))
                        {
                            if (t2.Status == TokenStatus.WAITING)
                            {
                                log.Info("UpdateOrJoinChecklistStatus: marking OR-join token {0} as READY because token {1} was removed", t2.TokenId, tok.TokenId);
                                t2.Status = TokenStatus.READY;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This function is responsible for updating net status after transition has fired.
        /// It consumes transition input tokens and produces tokens in transition output places.
        /// Also, transition input conditions are evaluated before creating output tokens.
        /// TODO: fix: in case when new token is created or token is removed from place that 
        /// belongs to some or-join's checklist, re-evaluate the or-join condition by 
        /// switching waiting tokens at that or-join to 'ready' status. We only need to handle 
        /// token removal as adding new tokens will not cause or join to fire.
        /// </summary>
        /// <param name="at"></param>
        private void UpdateNetStatusAfterTransition(TaskShell at)
        {
            Task tsk = Definition.GetTask(at.TaskId);
            foreach (string tokid in at.Tokens)
            {
                Token tok = GetToken(tokid);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ALLOCATED);
                Debug.Assert(tok.ActiveTransitions.Count == 1);
                Debug.Assert(tok.ActiveTransitions[0] == at.CorrelationId);
                tok.Status = TokenStatus.CONSUMED;
                UpdateOrJoinChecklistStatusAfterTokenRemoval(tok);
            }

            IList<Token> newTokens = new List<Token>();
            if (tsk.SplitType == JoinType.AND)
            {
                foreach (Flow fl in tsk.FlowsOut)
                {
                    if (fl.InputCondition != null && fl.InputCondition.Length > 0) throw new Exception();
                    Token t = CreateNewTokenInPlace(fl.To.Id);
                    newTokens.Add(t);
                }
            }
            else if (tsk.SplitType == JoinType.XOR)
            {
                IList<Flow> flows = tsk.FlowsOutOrdered;
                for (int i = 0; i < flows.Count; i++)
                {
                    if (i == flows.Count - 1) //last flow - the default one
                    {
                        Token t = CreateNewTokenInPlace(flows[i].To.Id);
                        log.Debug("Produced token in default flow: {0}, token: {1}", flows[i].ToString(), t.TokenId);
                        newTokens.Add(t);
                    }
                    else
                    {
                        if (EvaluateFlowInputCondition(flows[i]))
                        {
                            Token t = CreateNewTokenInPlace(flows[i].To.Id);
                            log.Debug("Produced token in flow: {0}, token: {1}", flows[i].ToString(), t.TokenId);
                            newTokens.Add(t);
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
                        Token t = CreateNewTokenInPlace(flows[i].To.Id);
                        log.Debug("Produced token in flow: {0}, token: {1}", flows[i].ToString(), t.TokenId);
                        newTokens.Add(t);
                    }
                }
            }
            else throw new Exception();
            if (newTokens.Count == 0) 
                throw new ApplicationException("No tokens were produced after transition " + at.CorrelationId);
            foreach (Token t in newTokens)
            {
                t.Status = TokenStatus.READY;
                AddToken(t);
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
            IScriptContext ctx = CreateProcessScriptContext();
            string expr = fl.InputCondition.Trim();
            if (!expr.EndsWith(";")) expr += ";";
            log.Debug("Evaluating flow {0} input condition: {1}", fl.ToString(), expr);
            object res = Script.RunCode(expr, ctx);
            log.Debug("Result: {0}", res);
            return Convert.ToBoolean(res);
        }

        /// <summary>
        /// Cancel active transition instance
        /// How do we do that?
        /// 1. Do activity-specific cancellation
        /// 2. Update status of each input token: if it has no more enabled transitions, put it into READY state. Otherwise, leave it in WAITING_TASK
        /// </summary>
        /// <param name="at"></param>
        private void CancelActiveTransition(TaskShell at)
        {
            log.Info("Cancelling transition {0}", at.CorrelationId);
            if (at.IsImmediate) throw new Exception("Cannot cancel an immediate transition");
            if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
            at.CancelTask();
            Debug.Assert(at.Status == TransitionStatus.CANCELLED);
            foreach (string t in at.Tokens)
            {
                Token tok = GetToken(t);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ENABLED || tok.Status == TokenStatus.LOCKED_ALLOCATED);
                Debug.Assert(tok.ActiveTransitions.Contains(at.CorrelationId));
                if (!tok.ActiveTransitions.Remove(at.CorrelationId)) throw new Exception("Error removing transition id from token"); //should never happen
                if (tok.ActiveTransitions.Count == 0)
                {
                    tok.Status = TokenStatus.READY;
                }
            }
            ActiveTransitionCancelled ac = new ActiveTransitionCancelled();
            ac.CorrelationId = at.CorrelationId;
            ac.DefinitionId = ProcessDefinitionId;
            ac.InstanceId = InstanceId;
            ac.TaskId = at.TaskId;
            ac.TaskType = Definition.GetTask(at.TaskId).GetType().Name;
            NotifyProcessEvent(ac);
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
            TaskShell ts = GetActiveTransition(correlationId);
            //1. transfer task output data from transition
            ts.TransferTaskOutputDataToParent(GetProcessVariablesContainer());
            ValidateProcessInternalData();
            //2. update network status
            this.AfterTransitionCompleted(correlationId);
        }

        #endregion

        

        
        

        /// <summary>
        /// Cancel process instance
        /// Cancels all currently active transitions and removes all tokens
        /// from the process.
        /// TODO: implement
        /// </summary>
        public void CancelProcessInstance()
        {
            log.Info("Cancelling process");
            foreach (Place pl in this.Definition.Places)
            {
                IList<Token> tokensInPlace = GetTokensInPlace(pl.Id);
                foreach (Token tok in tokensInPlace)
                {
                    if (tok.Status == TokenStatus.READY ||
                        tok.Status == TokenStatus.LOCKED_ENABLED ||
                        tok.Status == TokenStatus.LOCKED_ALLOCATED ||
                        tok.Status == TokenStatus.WAITING)
                    {
                        log.Debug("Cancelling token {0} in place {1}", tok.TokenId, pl.Id);
                        CancelToken(tok);
                    }
                }
            }
            _status = ProcessStatus.Cancelled;
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
            dob["TokenNumber"] = this._tokenNumber;
            dob["TransitionNumber"] = this._transitionNumber;

            List<object> al = new List<object>();
            foreach (string tId in _tokens.Keys)
            {
                Token tok = GetToken(tId);
                DataObject tdob = tok.SaveState();
                al.Add(tdob);
            }
            dob["Token"] = al;
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
            _processInstanceData = (DataObject) dob["InstanceData"];
            _correlationId = (string)dob["CorrelationId"];
            _tokenNumber = Convert.ToInt32(dob["TokenNumber"]);
            _transitionNumber = Convert.ToInt32(dob["TransitionNumber"]);
            IList l = dob.GetArray("Token");
            _tokens = new Dictionary<string, Token>();
            if (l != null)
            {
                foreach (DataObject dob2 in l)
                {
                    Token tok = new Token();
                    tok.RestoreState(dob2);
                    _tokens[tok.TokenId] = tok;
                }
            }
            _activeTransitions = new Dictionary<string, TaskShell>();
            l = dob.GetArray("Transition");
            if (l != null)
            {
                foreach (DataObject dob2 in l)
                {
                    TaskShell ts = TaskShell.RestoreTaskShell(dob2);
                    _activeTransitions[ts.CorrelationId] = ts;
                }
            }
            foreach (Token tok in _tokens.Values)
            {
                if (tok.Status != TokenStatus.CONSUMED &&
                    tok.Status != TokenStatus.CANCELLED)
                {
                    foreach (string atid in tok.ActiveTransitions)
                    {
                        if (!_activeTransitions.ContainsKey(atid)) throw new Exception("Invalid transition id: " + atid);
                    }
                }
            }
            foreach (TaskShell ts in _activeTransitions.Values)
            {
                if (ts.Status == TransitionStatus.ENABLED ||
                    ts.Status == TransitionStatus.STARTED)
                {
                    foreach (string tokid in ts.Tokens)
                    {
                        if (!_tokens.ContainsKey(tokid)) throw new Exception("Invalid token: " + tokid);
                    }
                }
            }
        }
    }

    
}

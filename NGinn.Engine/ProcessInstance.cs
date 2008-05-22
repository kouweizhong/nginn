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
    public class ProcessInstance
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
        [NonSerialized]
        private int _persistedVersion;
        [NonSerialized]
        private bool _activated = false;
        [NonSerialized]
        private INGEnvironmentContext _environment;
        [NonSerialized]
        private ActiveTransitionFactory _transitionFactory;
        /// <summary>map: correlation id->transition</summary>
        private IDictionary<string, ActiveTransition> _activeTransitions = new Dictionary<string, ActiveTransition>();
        /// <summary>helper map: task id -> list of active instances of the task</summary>
        [NonSerialized]
        private IDictionary<string, IList<ActiveTransition>> _activeTaskTransitions = new Dictionary<string, IList<ActiveTransition>>();
        private ProcessStatus _status;
        private int _tokenNumber = 0;
        private int _transitionNumber = 0;
        private DataObject _processInstanceData = new DataObject();
        [NonSerialized]
        private XmlDocument _processData = new XmlDocument();
        /// <summary>process xml data in string form - for serialization purposes</summary>
        private string _processDataXmlString = null;
        private string _correlationId;

        public ProcessInstance()
        {
            _status = ProcessStatus.Ready;
        }

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

        public ProcessStatus Status
        {
            get { return _status; }
        }

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
        /// Called before 'Activate' to insert token information into process instance
        /// </summary>
        /// <param name="activeTokens"></param>
        public void InitTokenInformation(ICollection<Token> tokens)
        {
            Dictionary<string, Token> d = new Dictionary<string, Token>();
            foreach (Token t in tokens)
            {
                if (t.ProcessInstanceId != this.InstanceId) throw new Exception("Token with invalid instance id");
                d[t.TokenId] = t;
            }
            _tokens = d;
            _tokensInPlaces = null;
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
            Dictionary<string, IList<ActiveTransition>> newTransitions = new Dictionary<string, IList<ActiveTransition>>();
            foreach (ActiveTransition at in this._activeTransitions.Values)
            {
                IList<ActiveTransition> ats;
                if (!newTransitions.TryGetValue(at.TaskId, out ats))
                {
                    ats = new List<ActiveTransition>();
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
        private string GetNextTransitionId()
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
            t.ProcessInstanceId = this.InstanceId;
            t.PlaceId = this.Definition.GetPlace(placeId).Id;
            t.Status = TokenStatus.READY;
            t.Mode = TokenMode.LIVE;
            t.PersistedVersion = 0;
            t.Dirty = true;
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
        private void NotifyProcessEvent(ProcessEvent pe)
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
                if (tok.ProcessInstanceId != this.InstanceId) throw new Exception("Invalid process instance id");
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
        /// Initialize new script execution context for this process instance
        /// </summary>
        /// <returns></returns>
        protected IScriptContext CreateProcessScriptContext(IDataObject variables)
        {
            IScriptContext ctx = new ScriptContext();
            ctx.SetItem("_processDef", ContextItem.Variable, Definition);
            ctx.SetItem("_instance", ContextItem.Variable, this);
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
        /// Set process input data
        /// </summary>
        /// <param name="data"></param>
        public void SetProcessInputData(IDataObject data)
        {
            StructDef procInput = Definition.GetProcessInputDataSchema();
            data.Validate(procInput);
            DataObject dob = new DataObject();
            
            IScriptContext ctx = CreateProcessScriptContext(null);
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
            if (tok == null) return false;
            KickToken(tok.TokenId);
            return Status == ProcessStatus.Ready;
        }

        /// <summary>
        /// Passivate is called before persisting the process instance data
        /// </summary>
        public void Passivate()
        {
            log.Info("Passivating");
            if (_processData != null)
            {
                _processDataXmlString = _processData.OuterXml;
            }
            _tokensInPlaces = null;
            _definition = null;
            _environment = null;
            _activated = false;
        }
        
        /// <summary>
        /// Activate is called after process instance is deserialized, but before any operations
        /// are performed on it.
        /// </summary>
        public void Activate()
        {
            if (_activated) throw new Exception("Process instance already activated");
            if (Environment == null) throw new Exception("Environment not initialized. Please set the 'Environment' property");
            log = LogManager.GetLogger(string.Format("ProcessInstance.{0}", InstanceId));
            _transitionFactory = new ActiveTransitionFactory();
            _definition = Environment.DefinitionRepository.GetProcessDefinition(ProcessDefinitionId);
            if (_processDataXmlString != null)
            {
                //_processData = new XmlDocument();
                //_processData.LoadXml(_processDataXmlString);
            }
            BuildTokensInPlaces();
            BuildActiveTransitionsInTasks();
            foreach (ActiveTransition at in _activeTransitions.Values)
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
        private IList<ActiveTransition> GetActiveInstancesOfTask(string taskId)
        {
            IList<ActiveTransition> lst;
            if (_activeTaskTransitions.TryGetValue(taskId, out lst))
                return lst;
            return new List<ActiveTransition>();
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
            if (tok.Status != TokenStatus.READY) throw new Exception("invalid status");
            
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
            //wersja pe³na
            //wchodzi token. Dla kazdego wychodzacego tasku: 
            //  nEn = ile task ma enabled tranzycji. 
            //  sprawdz, czy task mozna enablowac, ale przy zalozeniu ze nie bierzemy tokenow z tranzycji dla tego tasku 
            //  ktore juz sa enablowane
            IList<ActiveTransition> newAts = new List<ActiveTransition>();

            foreach (Task tsk in pl.NodesOut)
            {
                //1. calculating set of tokens that were used for enabling tsk 
                //   so we don't use them for enabling next instance of it
                IDictionary<string, string> tokDict = new Dictionary<string, string>();
                IList<ActiveTransition> taskTransitions = GetActiveInstancesOfTask(tsk.Id);
                foreach (ActiveTransition at in taskTransitions)
                {
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
                    ActiveTransition at = CreateActiveTransitionForTask(tsk);
                    foreach (Token t in enablingTokens)
                    {
                        at.Tokens.Add(t.TokenId);
                    }

                    if (tsk.IsImmediate)
                    {
                        log.Info("Task {0} is immediate. Will start it now.", tsk.Id);
                        newAts.Clear();
                        newAts.Add(at);
                        break; //leave the loop
                    }
                    else
                    {
                        newAts.Add(at);
                    }
                }
            }

            if (newAts.Count == 0)
            {
                log.Info("No new transitions can be initiated. Marking token {0} as WAITING", tok.ToString());
                tok.Status = TokenStatus.WAITING;
                return;
            }

            //ok, now start the transitions
            //but before starting, find all transitions that share tokens and 'link' them together
            foreach (ActiveTransition at in newAts)
            {
                foreach (string tokId in at.Tokens)
                {
                    Token t = GetToken(tokId);
                    
                }

                if (at.IsImmediate)
                {
                    AddActiveTransition(at);
                    ExecuteTransition(at);
                }
                else
                {
                    AddActiveTransition(at);
                    InitiateTransition(at);
                    log.Info("Initiated transition {0}: {1}->{2}", at.CorrelationId, tok.PlaceId, at.TaskId);
                    foreach (string tokId in at.Tokens)
                    {
                        Token tok1 = GetToken(tokId);
                        tok1.Status = TokenStatus.LOCKED_ENABLED;
                        log.Info("Changed status of token ({0}) to {1}", tok1.TokenId, tok1.Status);
                    }
                }
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
                    if (!foundTokInPlace)
                    {
                        return false;
                    }
                }
                return true;
            }
            else throw new Exception();
        }

        /// <summary>
        /// List of 'consumable' tokens, that is tokens suitable for being consumed by a transition,
        /// or for initiating a transition. It consists of READY, WAITING and WAITING_TASK tokens.
        /// After initiating a transition, they become WAITING_TASK.
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
        private void InitiateTransition(ActiveTransition at)
        {
            log.Info("Initiating transition {0}", at.TaskId);
            TransferDataToTransition(at);
            at.InitiateTask();
        }

        /// <summary>
        /// Execute immediate transition
        /// </summary>
        /// <param name="at"></param>
        private void ExecuteTransition(ActiveTransition at)
        {
            Debug.Assert(at.IsImmediate);
            TransferDataToTransition(at);
            at.ExecuteTask();
            foreach (string tokId in at.Tokens)
            {
                Token tok1 = GetToken(tokId);
                tok1.Status = TokenStatus.LOCKED_ENABLED;
            }
            TransitionCompleted(at.CorrelationId);
        }

        
        /// <summary>
        /// Create input data for transition by executing task input data bindings.
        /// TODO: Implement
        /// All task input variables have to be bound...
        /// 1. execute data bindings - get input variables in xml
        /// 2. validate the xml document
        /// 3. add local variables initial values
        /// </summary>
        /// <param name="at"></param>
        private void TransferDataToTransition(ActiveTransition at)
        {
            log.Debug("Transferring data to transition {0}", at.CorrelationId);
            IScriptContext ctx = CreateProcessScriptContext(GetProcessVariablesContainer());
            DataObject taskInput = new DataObject();
            DataBinding.ExecuteDataBinding(taskInput, Definition.GetTask(at.TaskId).InputBindings, ctx);
            at.SetTaskInputData(taskInput);
        }

        /// <summary>
        /// Extract output data from finished transition by executing task output data bindings
        /// </summary>
        /// <param name="at"></param>
        private void TransferDataFromTransition(ActiveTransition at)
        {
            IDataObject dob = at.GetTaskOutputData();
            IScriptContext ctx = CreateProcessScriptContext(dob);
            IDataObject target = GetProcessVariablesContainer();
            Task tsk = Definition.GetTask(at.TaskId);
            DataBinding.ExecuteDataBinding(target, tsk.OutputBindings, ctx);
            ValidateProcessInternalData();
        }


        /// <summary>
        /// Return xml namespace manager of process data xml
        /// </summary>
        /// <returns></returns>
        protected XmlNamespaceManager GetProcessDataNamespaceManager()
        {
            return new XmlNamespaceManager(_processData.NameTable);
        }
        

        

        private ActiveTransition CreateActiveTransitionForTask(Task tsk)
        {
            ActiveTransition at = _transitionFactory.CreateTransition(this, tsk);
            at.CorrelationId = GetNextTransitionId();
            at.Activate();
            return at;
        }

        /// <summary>
        /// Add initialized active transition to the collection of active transitions
        /// </summary>
        /// <param name="at"></param>
        private void AddActiveTransition(ActiveTransition at)
        {
            if (at.CorrelationId == null) throw new Exception("Correlation id required");
            if (at.Tokens.Count == 0) throw new Exception("Cannot add a transition without tokens");
            Debug.Assert(InstanceId == at.ProcessInstanceId);
            Debug.Assert(Definition.GetTask(at.TaskId) != null);
            //find shared transitions...
            IDictionary<string, ActiveTransition> sharedTrans = new Dictionary<string, ActiveTransition>();
            foreach (string tid in at.Tokens)
            {
                Token t = GetToken(tid);
                foreach (string atid in t.ActiveTransitions)
                {
                    if (!sharedTrans.ContainsKey(atid)) 
                        sharedTrans[atid] = GetActiveTransition(atid);
                }
            }
            List<ActiveTransition> strans = new List<ActiveTransition>(sharedTrans.Values);
            if (strans.Count > 0)
            {
                log.Info("Transition shares the same tokens with {0} active transitions.");
                if (strans.Count == 1)
                {
                    strans[0].SharedId = strans[0].CorrelationId;
                }
                at.SharedId = strans[0].SharedId;
                foreach (ActiveTransition at2 in strans) Debug.Assert(at2.SharedId == at.SharedId);
            }

            Task tsk = Definition.GetTask(at.TaskId);
            _activeTransitions[at.CorrelationId] = at;
            IList<ActiveTransition> ats;
            if (!_activeTaskTransitions.TryGetValue(at.TaskId, out ats))
            {
                ats = new List<ActiveTransition>();
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
        private IList<ActiveTransition> GetSharedActiveTransitionsForTransition(ActiveTransition at)
        {
            IDictionary<string, ActiveTransition> dict = new Dictionary<string, ActiveTransition>();
            foreach (string tokid in at.Tokens)
            {
                Token t = GetToken(tokid);
                foreach (string atid in t.ActiveTransitions)
                {
                    if (dict.ContainsKey(atid)) continue;
                    ActiveTransition at2 = GetActiveTransition(atid);
                    if (at2 == at) continue;
                    if (at2.Status == TransitionStatus.ENABLED)
                    {
                        dict[atid] = at2;
                    }
                }
            }
            return new List<ActiveTransition>(dict.Values);
        }

        /// <summary>
        /// Report that a process task has completed
        /// </summary>
        /// <param name="tci"></param>
        public void TransitionCompleted(TaskCompletionInfo tci)
        {
            if (tci.ProcessInstance != this.InstanceId) throw new Exception("Invalid instance id");
            ActiveTransition at = GetActiveTransition(tci.CorrelationId);
            if (at == null) throw new Exception("Invalid correlation id");
            if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
            //1 update task data
            if (tci.ResultXml != null)
            {
                DataObject dob = DataObject.ParseXml(tci.ResultXml);
                at.UpdateTaskData(dob);
            }
            //2 complete the transition
            TransitionCompleted(tci.CorrelationId);
        }

        /// <summary>
        /// Select a transition for executing (used for deciding which deferred choice alternative has been selected)
        /// </summary>
        /// <param name="correlationId"></param>
        public void TransitionSelected(string correlationId)
        {
            ActiveTransition at = GetActiveTransition(correlationId);
            if (at == null) throw new Exception("Invalid correlation id");
            if (at.Status != TransitionStatus.ENABLED) throw new Exception("Invalid transition status");
            log.Info("Transition selected: {0}", at.CorrelationId);
            Task tsk = Definition.GetTask(at.TaskId);
            //1 find all 'shared' transitions and cancel them
            IList<ActiveTransition> sharedTrans = GetSharedActiveTransitionsForTransition(at);
            if (sharedTrans.Count > 0)
            {
                log.Info("Found {0} active transitions to cancel", sharedTrans.Count);
                foreach (ActiveTransition at2 in sharedTrans)
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
            at.Status = TransitionStatus.STARTED;
        }


        /// <summary>
        /// Transition completed - consume input tokens, produce output tokens
        /// and cancel all transitions that share the same tokens.
        /// Also, if some tokens were waiting, put them in 'READY' state.
        /// Wow, looks quite complex.
        /// </summary>
        /// <param name="at"></param>
        private void TransitionCompleted(string correlationId)
        {
            ActiveTransition at = _activeTransitions[correlationId];
            log.Info("Transition completed: {0}", at.CorrelationId);
            Task tsk = Definition.GetTask(at.TaskId);
            //1 select the transition for processing
            if (at.Status == TransitionStatus.ENABLED)
            {
                TransitionSelected(at.CorrelationId);
            }
            Debug.Assert(GetSharedActiveTransitionsForTransition(at).Count == 0);
            foreach (string tokid in at.Tokens)
            {
                Token tok = GetToken(tokid);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ALLOCATED);
            }
            
            at.TaskCompleted();
            at.Status = TransitionStatus.COMPLETED;
            //2 retrieve data from transition
            TransferDataFromTransition(at);
            //3 move the tokens
            UpdateNetStatusAfterTransition(at);
        }

        /// <summary>
        /// This function is responsible for updating net status after transition has fired.
        /// It consumes transition input tokens and produces tokens in transition output places.
        /// Also, transition input conditions are evaluated before creating output tokens.
        /// </summary>
        /// <param name="at"></param>
        private void UpdateNetStatusAfterTransition(ActiveTransition at)
        {
            Task tsk = Definition.GetTask(at.TaskId);
            foreach (string tokid in at.Tokens)
            {
                Token tok = GetToken(tokid);
                Debug.Assert(tok.Status == TokenStatus.LOCKED_ALLOCATED);
                Debug.Assert(tok.ActiveTransitions.Count == 1);
                Debug.Assert(tok.ActiveTransitions[0] == at.CorrelationId);
                tok.Status = TokenStatus.CONSUMED;
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
            IScriptContext ctx = CreateProcessScriptContext(GetProcessVariablesContainer());
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
        private void CancelActiveTransition(ActiveTransition at)
        {
            log.Info("Cancelling transition {0}", at.CorrelationId);
            if (at.IsImmediate) throw new Exception("Cannot cancel an immediate transition");
            if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
            at.CancelTask();
            at.Status = TransitionStatus.CANCELLED;
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
        private ActiveTransition GetActiveTransition(string correlationId)
        {
            ActiveTransition at = null;
            _activeTransitions.TryGetValue(correlationId, out at);
            return at;
        }

        public IDataObject GetTaskData(string correlationId)
        {
            ActiveTransition at = GetActiveTransition(correlationId);
            return at.GetTaskData();
        }

 
        private string ToXmlString()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings sett = new XmlWriterSettings();
            sett.Indent = true;
            sett.OmitXmlDeclaration = true;
            XmlWriter xw = XmlWriter.Create(sb, sett);
            xw.WriteStartElement("ProcessInstance");
            xw.WriteAttributeString("id", InstanceId);
            xw.WriteAttributeString("definitionId", ProcessDefinitionId);
            xw.WriteAttributeString("definitionName", Definition.Name);
            xw.WriteAttributeString("definitionVersion", Definition.Version.ToString());
            xw.WriteStartElement("Tokens");
            foreach (Token tok in _tokens.Values)
            {
                xw.WriteStartElement("Token");
                xw.WriteAttributeString("id", tok.TokenId);
                xw.WriteAttributeString("place", tok.PlaceId);
                xw.WriteAttributeString("status", tok.Status.ToString());
                xw.WriteAttributeString("mode", tok.Mode.ToString());
                foreach (string at in tok.ActiveTransitions) xw.WriteElementString("Transition", at);
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteStartElement("Transitions");
            foreach (ActiveTransition at in _activeTransitions.Values)
            {
                xw.WriteStartElement("Transition");
                xw.WriteAttributeString("correlationId", at.CorrelationId);
                xw.WriteAttributeString("type", at.GetType().Name);
                xw.WriteAttributeString("taskId", at.TaskId);
                xw.WriteAttributeString("status", at.Status.ToString());
                foreach (string t in at.Tokens) xw.WriteElementString("Token", t);
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.Flush();
            return sb.ToString();
        }
        

        public override string ToString()
        {
            return ToXmlString();
        }
    }

    
}

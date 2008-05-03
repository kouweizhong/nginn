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

namespace NGinn.Engine
{
    /// <summary>
    /// Enumeration of possible process instance statuses
    /// </summary>
    public enum ProcessStatus
    {
        New = 0,
        Active,
        Error,
        Finished,
        Cancelled
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
        [NonSerialized]
        private XmlDocument _processData = new XmlDocument();
        /// <summary>process xml data in string form - for serialization purposes</summary>
        private string _processDataXmlString = null;

        public ProcessInstance()
        {
            _status = ProcessStatus.New;
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
                    t.Status == TokenStatus.WAITING_ALLOCATED ||
                    t.Status == TokenStatus.WAITING_ENABLED)
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
            NotifyProcessEvent(ps);
        }

        /// <summary>
        /// Send a notification about process event
        /// </summary>
        /// <param name="pe"></param>
        private void NotifyProcessEvent(ProcessEvent pe)
        {
            Environment.MessageBus.Notify("ProcessInstance", "ProcessInstance.Event." + pe.GetType().Name, pe, true);
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
                if (pl == Definition.Start && _status == ProcessStatus.New && _tokens.Count == 0)
                {
                    _status = ProcessStatus.Active;
                    OnProcessStarted();
                }
            }
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
            XmlDocument doc = new XmlDocument();
            XmlDocument d2 = new XmlDocument();
            d2.LoadXml(inputXml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(d2.NameTable);
            
            XmlElement root = doc.CreateElement("process");
            doc.AppendChild(root);
            
            XmlElement instid = doc.CreateElement("processInstance");
            instid.InnerText = this.InstanceId;
            root.AppendChild(instid);
            instid = doc.CreateElement("processDefinition");
            instid.InnerText = this.ProcessDefinitionId;
            root.AppendChild(instid);
            XmlElement vroot = doc.CreateElement("inputData");
            root.AppendChild(vroot);
            XmlNode curChild = null;
            foreach(VariableDef vd in Definition.ProcessVariables)
            {
                log.Debug("Inserting variable {0}", vd.Name);
                XmlNodeList nodes = d2.DocumentElement.SelectNodes(vd.Name, nsmgr);
                List<XmlNode> newNodes = new List<XmlNode>();
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode xn in nodes)
                    {
                        newNodes.Add(doc.ImportNode(xn, true));
                    }
                }
                else
                {
                    if (vd.VariableUsage == VariableDef.Usage.Required) throw new ApplicationException("Required variable is missing: " + vd.Name);
                    XmlNode xn = doc.CreateElement(vd.Name);
                    if (vd.DefaultValueExpr != null) xn.InnerText = vd.DefaultValueExpr;
                    newNodes.Add(xn);
                }
                foreach(XmlNode xn in newNodes)
                {
                    curChild = vroot.InsertAfter(xn, curChild);
                }
            }
            this._processData = doc;
            log.Info("Process data: {0}", doc.OuterXml);
        }

        /// <summary>
        /// Return process instance data xml
        /// </summary>
        /// <returns></returns>
        public XmlDocument GetProcessData()
        {
            return this._processData;
        }

        /// <summary>
        /// Return node where process variables are kept
        /// </summary>
        /// <returns></returns>
        public XmlNode GetProcessVariablesRoot()
        {
            return GetProcessData().DocumentElement.SelectSingleNode("inputData");
        }

        /// <summary>
        /// Set process instance data xml
        /// Warning: this function does not validate the xml structure, so incorrect xml will corrupt the process logic.
        /// </summary>
        /// <param name="data"></param>
        public void SetProcessData(XmlDocument data)
        {
            this._processData = data;
        }

        /// <summary>
        /// Returns process output xml. Can be used only after process is completed.
        /// </summary>
        /// <returns></returns>
        public XmlDocument GetProcessOutputXml()
        {
            if (this.Status != ProcessStatus.Finished) throw new ApplicationException("Cannot return output xml - process did not finish");
            throw new NotImplementedException();
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
            KickReadyToken(tok);
            return true;
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
                _processData = new XmlDocument();
                _processData.LoadXml(_processDataXmlString);
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
            NotifyProcessEvent(pf);
        }

        /// <summary>
        /// Kick a 'READY' token. It means-> initiate all tasks following current place - only if the tasks can be initiated.
        /// </summary>
        /// <param name="tok"></param>
        public void KickReadyToken(Token tok)
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
                if (tok1.Status == TokenStatus.WAITING_ENABLED)
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
                        tok1.Status = TokenStatus.WAITING_ENABLED;
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
                else if (t.Status == TokenStatus.WAITING_ENABLED)
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
                tok1.Status = TokenStatus.WAITING_ENABLED;
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
            XmlElement nd = (XmlElement) GetProcessVariablesRoot();
            Task tsk = Definition.GetTask(at.TaskId);

            XmlDocument newDoc = new XmlDocument();
            XmlElement taskData = newDoc.CreateElement("input");
            taskData = (XmlElement) newDoc.AppendChild(taskData);
            IDictionary<string, IList<XmlElement>> bindingResults = XmlProcessingUtil.EvaluateVariableBindings(nd, tsk.InputBindings);
            foreach (VariableDef vd in tsk.TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    IList<XmlElement> value;
                    if (bindingResults.TryGetValue(vd.Name, out value))
                    {
                        foreach (XmlElement el in value)
                        {
                            taskData.AppendChild(taskData.OwnerDocument.ImportNode(el, true));
                        }
                    }
                }
            }
            log.Info("Task input data: {0}", taskData.OuterXml);
            at.SetTaskInputXml(newDoc.OuterXml);
        }


        /// <summary>
        /// Return xml namespace manager of process data xml
        /// </summary>
        /// <returns></returns>
        protected XmlNamespaceManager GetProcessDataNamespaceManager()
        {
            return new XmlNamespaceManager(_processData.NameTable);
        }
        

        /// <summary>
        /// Extract output data from finished transition by executing task output data bindings
        /// </summary>
        /// <param name="at"></param>
        private void TransferDataFromTransition(ActiveTransition at)
        {
            log.Debug("Transferring data from transition {0}", at.CorrelationId);
            Task tsk = Definition.GetTask(at.TaskId);
            XmlNode nd = at.TaskVariablesRoot;
            IDictionary<string, IList<XmlElement>> bindingResults = XmlProcessingUtil.EvaluateVariableBindings(nd, tsk.OutputBindings);
            XmlNamespaceManager nsmgr = GetProcessDataNamespaceManager();
            IDictionary<string, IList<XmlElement>> processVars = XmlProcessingUtil.RetrieveVariablesFromXml(this.GetProcessVariablesRoot(), Definition.ProcessVariables, nsmgr);
            throw new NotImplementedException();
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
                Debug.Assert(tok.Status == TokenStatus.WAITING_ENABLED);
                Debug.Assert(tok.ActiveTransitions.Count == 1 && tok.ActiveTransitions[0] == at.CorrelationId);
                //if (!at.IsImmediate && tok.Status != TokenStatus.WAITING_ENABLED) throw new Exception();
                tok.Status = TokenStatus.WAITING_ALLOCATED;

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
                Debug.Assert(tok.Status == TokenStatus.WAITING_ALLOCATED);
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
                Debug.Assert(tok.Status == TokenStatus.WAITING_ALLOCATED);
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
            throw new NotImplementedException();
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
                Debug.Assert(tok.Status == TokenStatus.WAITING_ENABLED || tok.Status == TokenStatus.WAITING_ALLOCATED);
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

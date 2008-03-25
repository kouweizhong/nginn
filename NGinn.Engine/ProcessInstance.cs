using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using Spring.Context;
using NGinn.Engine.Services;
using System.Diagnostics;

namespace NGinn.Engine
{
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
        private IDictionary<string, object> _processVariables = new Dictionary<string, object>();
        [NonSerialized]
        private IDictionary<string, IList<Token>> _tokensInPlaces = new Dictionary<string, IList<Token>>();
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
        /// <summary>map: task id -> list of active instances of the task</summary>
        [NonSerialized]
        private IDictionary<string, IList<ActiveTransition>> _activeTaskTransitions = new Dictionary<string, IList<ActiveTransition>>();
        
        private int _tokenNumber = 0;
        private int _transitionNumber = 0;

        public string ProcessDefinitionId
        {
            get { return _definitionId; }
            set { _definitionId = value; }
        }

        public ProcessDefinition Definition
        {
            get { return _definition; }
        }

        public string InstanceId
        {
            get { return _instId; }
            set 
            { 
                _instId = value;
                log = LogManager.GetLogger(string.Format("ProcessInstance.{0}", value));
            }
        }

        public int PersistedVersion
        {
            get { return _persistedVersion; }
            set { _persistedVersion = value; }
        }

        public IDictionary<string, object> ProcessVariables
        {
            get { return _processVariables; }
            set { _processVariables = value; }
        }

        /// <summary>
        /// true if process has not finished yet (has tokens that did not reach end place)
        /// </summary>
        public bool IsAlive
        {
            get { return false; }
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
            return null;
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
                _tokens[tok.TokenId] = tok;
                IList<Token> lst;
                if (!_tokensInPlaces.TryGetValue(tok.PlaceId, out lst))
                {
                    lst = new List<Token>();
                    _tokensInPlaces[tok.PlaceId] = lst;
                }
                lst.Add(tok);
            }
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
            BuildTokensInPlaces();
            BuildActiveTransitionsInTasks();
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
            IList<Token> toks = GetActiveTokensInPlace(tok.PlaceId);
            //Strategy 1: if there are enabled transition (that is, tokens in WAITING_TASK state), 
            //do nothing and put the token in WAITING state. So actually we will be waiting for the transition
            //to fire before enabling other transitions
            foreach (Token tok1 in toks)
            {
                if (tok1.Status == TokenStatus.WAITING_TASK)
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
                    throw new Exception("not implemented yet");
                }
                else
                {
                    InitiateTransition(at);
                    log.Info("Initiated transition {0}: {1}->{2}", at.CorrelationId, tok.PlaceId, at.TaskId);
                    AddActiveTransition(at);
                    foreach (string tokId in at.Tokens)
                    {
                        Token tok1 = GetToken(tokId);
                        tok1.Status = TokenStatus.WAITING_TASK;
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
                    IList<Token> toksInPlace = GetActiveTokensInPlace(pl.Id);
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
                    IList<Token> toksInPlace = GetActiveTokensInPlace(pl.Id);
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
                    IList<Token> toksInPlace = GetActiveTokensInPlace(pl.Id);
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
        /// List of 'active' tokens, that is tokens suitable for being consumed by a transition,
        /// or for initiating a transition. It consists of READY, WAITING and WAITING_TASK tokens.
        /// After initiating a transition, they become WAITING_TASK.
        /// If the transition is immediate, it will execute and consume the tokens.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        private IList<Token> GetActiveTokensInPlace(string placeId)
        {
            List<Token> lst = new List<Token>();
            List<Token> l2 = new List<Token>();
            IList<Token> toks = GetTokensInPlace(placeId);
            foreach (Token t in toks)
            {
                if (t.Status == TokenStatus.READY || t.Status == TokenStatus.WAITING)
                    lst.Add(t);
                else if (t.Status == TokenStatus.WAITING_TASK)
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
            at.InitiateTask();
        }

        /// <summary>
        /// Execute immediate transition
        /// </summary>
        /// <param name="at"></param>
        private void ExecuteTransition(ActiveTransition at)
        {
            Debug.Assert(at.IsImmediate);
            at.InitiateTask();
        }

        private void TryInitiateTask(ActiveTransition at)
        {
            Task tsk = Definition.GetTask(at.TaskId);
            //what now? every task should have its 'active' counterpart.

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
            Task t = Definition.GetTask(at.TaskId);
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
            //1 find all 'shared' transitions
            foreach (string tid in at.Tokens)
            {
                Token tok = GetToken(tid);
                log.Info("Checking shared transitions for token {0}", tok.ToString());
            }         

        }

       
        private void CancelTransition(ActiveTransition at)
        {
        }
    }
}

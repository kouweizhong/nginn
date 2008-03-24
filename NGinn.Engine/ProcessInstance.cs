using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using Spring.Context;
using NGinn.Engine.Services;

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
        [NonSerialized]
        private IApplicationContext _ctx;
        private IDictionary<string, object> _processVariables = new Dictionary<string, object>();
        [NonSerialized]
        private IDictionary<string, IList<Token>> _tokensInPlaces = new Dictionary<string, IList<Token>>();
        private IDictionary<string, Token> _tokens = new Dictionary<string, Token>();
        [NonSerialized]
        private int _persistedVersion;
        [NonSerialized]
        private bool _activated = false;
        [NonSerialized]
        private INGEnvironment _environment;
        [NonSerialized]
        private ActiveTransitionFactory _transitionFactory = new ActiveTransitionFactory();

        private IList<ActiveTransition> _activeTransitions = new List<ActiveTransition>();
        private int _tokenNumber = 0;
        
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
        public INGEnvironment Environment
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
            _ctx = null;
            _definition = null;
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
            IProcessDefinitionRepository rep = (IProcessDefinitionRepository) _ctx.GetObject("ProcessDefinitionRepository");
            _definition = rep.GetProcessDefinition(ProcessDefinitionId);
            BuildTokensInPlaces();
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
        /// Kick a 'READY' token. It means-> initiate all tasks following current place - only if the tasks can be initiated.
        /// 
        /// </summary>
        /// <param name="tok"></param>
        public void KickReadyToken(Token tok)
        {
            if (!_activated) throw new Exception("Process instance not activated");
            log.Info("Kicking token {0}", tok.ToString());
            if (_tokens[tok.TokenId] != tok) throw new Exception("invalid token");
            if (tok.Status != TokenStatus.READY) throw new Exception("invalid status");
            
            Place pl = Definition.GetPlace(tok.PlaceId);
            List<ActiveTransition> ats = new List<ActiveTransition>();
            foreach (Task tsk in pl.NodesOut)
            {
                //ActiveTransition at = CreateActiveTransitionForTask(tsk);
                //now let's select input tokens for the transition
                
                bool b = CanInitiateTransition(tsk);
                log.Info("Checking if transition {0} can be initiated: {1}", tsk.Id, b);
                if (b)
                {
                    ActiveTransition at = CreateActiveTransitionForTask(tsk);
                    at.ProcessInstanceId = this.InstanceId;
                    at.Status = TransitionStatus.Initiated;
                    
                    if (tsk.IsImmediate)
                    {
                        log.Info("Task {0} is immediate. Will start it now.", tsk.Id);
                        ats.Clear();
                        ats.Add(at);
                        break; //leave the loop
                    }
                    else
                    {
                        ats.Add(at);
                    }
                }
            }
            if (ats.Count == 0)
            {
                log.Info("No transitions can be initiated. Marking token as WAITING");
                tok.Status = TokenStatus.WAITING;
                return;
            }

            foreach (ActiveTransition at in ats)
            {
                InitiateTransition(at);
                log.Info("Initiated transition {0}->{1}", tok.PlaceId, at.TaskId);
            }


            foreach (ActiveTransition at in ats)
            {
                foreach (string tokId in at.Tokens)
                {
                    Token tok1 = GetToken(tokId);
                    tok1.ActiveTransitions.Add(at);
                    tok1.Status = TokenStatus.WAITING_TASK;
                    log.Info("Changed status of token ({0}) to {1}", tok1.TokenId, tok1.Status);
                }
            }
        }

        /// <summary>
        /// Transition can be initiated if it has required number (depending on join type) of tokens in statuses:
        /// READY, WAITING or WAITING_TASK
        /// </summary>
        /// <param name="tsk"></param>
        /// <returns></returns>
        private bool CanInitiateTransition(Task tsk)
        {
            if (tsk.JoinType != JoinType.AND) throw new Exception("JOin type not suported");
            foreach (Place pl in tsk.NodesIn)
            {
                if (GetActiveTokensInPlace(pl.Id).Count == 0)
                    return false;
            }
            return true;
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
            Task tsk = Definition.GetTask(at.TaskId);
            at.Tokens = new List<string>();
            if (tsk.JoinType == JoinType.AND)
            {
                foreach (Place p in tsk.NodesIn)
                {
                    IList<Token> toks = GetActiveTokensInPlace(p.Id);
                    if (toks.Count == 0) throw new Exception("Cannot initiate transition - no required token");
                    at.Tokens.Add(toks[0].TokenId);
                }
            }
            else if (tsk.JoinType == JoinType.XOR)
            {
                foreach (Place p in tsk.NodesIn)
                {
                    IList<Token> toks = GetActiveTokensInPlace(p.Id);
                    if (toks.Count > 0)
                    {
                        at.Tokens.Add(toks[0].TokenId);
                        break;
                    }
                }
            }
            else throw new Exception("Only AND and XOR join supported");
            //ok, tokens found. So now - please, start the transition
            at.InitiateTask();
        }

        private void TryInitiateTask(ActiveTransition at)
        {
            Task tsk = Definition.GetTask(at.TaskId);
            //what now? every task should have its 'active' counterpart.

        }

        private ActiveTransition CreateActiveTransitionForTask(Task tsk)
        {
            return _transitionFactory.CreateTransition(this, tsk);
        }
    }
}

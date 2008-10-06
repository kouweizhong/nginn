using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NGinn.Lib.Schema;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Data;
using NGinn.Engine.Runtime.Tasks;
using NGinn.Engine.Services;
using System.Diagnostics;
using System.IO;
using System.Xml;
using ScriptNET;

namespace NGinn.Engine.Runtime
{
    [Serializable]
    public class ProcessInstance2 : IProcessTransitionCallback
    {
        private Dictionary<string, int> _currentMarking = new Dictionary<string, int>();
        private string _definitionId;
        private string _instanceId;
        private List<string> _newTokens = new List<string>();
        private Dictionary<string, ActiveTransition> _activeTransitions = new Dictionary<string, ActiveTransition>();

        [NonSerialized]
        private INGEnvironmentContext _envContext;
        [NonSerialized]
        private ProcessDefinition _definition;
        [NonSerialized]
        private Logger log = LogManager.GetCurrentClassLogger();

        public ProcessInstance2()
        {
            
        }

        public string InstanceId
        {
            get { return _instanceId; }
            set { _instanceId = value; }
        }

        public string ProcessDefinitionId
        {
            get { return _definitionId; }
            set { _definitionId = value; }
        }

        protected ProcessDefinition Definition
        {
            get { return _definition; }
        }

        public INGEnvironmentContext EnvironmentContext
        {
            get { return _envContext; }
            set { _envContext = value; }
        }


        public void Activate()
        {
            if (_envContext == null) throw new Exception("Environment not set");
            if (_instanceId == null) throw new Exception("Instance Id not set");
            if (_definitionId == null) throw new Exception("Definition not set");
            log = LogManager.GetCurrentClassLogger();
            _definition = _envContext.DefinitionRepository.GetProcessDefinition(_definitionId);

        }
        /// <summary>
        /// Return number of tokens in given place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public int GetNumTokens(string placeId)
        {
            int n = 0;
            return _currentMarking.TryGetValue(placeId, out n) ? n : 0;
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
                _newTokens.Add(placeId);
            }
        }

        /// <summary>
        /// Remove a token from place
        /// </summary>
        /// <param name="placeId"></param>
        public void RemoveToken(string placeId)
        {
            lock (this)
            {
                int n = GetNumTokens(placeId);
                if (n <= 0) throw new Exception("No tokens in " + placeId);
                _currentMarking.Remove(placeId);
                _currentMarking[placeId] = n--;
            }
        }

        /// <summary>
        /// Select place containing some tokens ready for processing
        /// </summary>
        /// <returns></returns>
        private string SelectPlaceWithReadyTokens()
        {
            lock (this)
            {
                while (_newTokens.Count > 0)
                {
                    string s = _newTokens[0];
                    _newTokens.RemoveAt(0);
                    if (GetNumTokens(s) > 0) return s;
                }
                return null;
            }
        }

        private ActiveTransition GetActiveInstanceOfTask(string taskDefId)
        {
            foreach (ActiveTransition ts in _activeTransitions.Values)
            {
                if (ts.TaskId == taskDefId && (ts.Status == TransitionStatus.ENABLED || ts.Status == TransitionStatus.STARTED))
                    return ts;
            }
            return null;
        }

        private ActiveTransition GetActiveTransition(string correlationId)
        {
            ActiveTransition at = null;
            return _activeTransitions.TryGetValue(correlationId, out at) ? at : null;
        }

        public void Kick()
        {
            string plId = SelectPlaceWithReadyTokens();
            if (plId == null)
            {
                log.Info("No ready tokens");
                return;
            }
            Place pl = Definition.GetPlace(plId);
            Debug.Assert(pl != null);
            int nToks = GetNumTokens(plId);
            Debug.Assert(nToks > 0);
            foreach (Task tsk in pl.NodesOut)
            {
                ActiveTransition ts = GetActiveInstanceOfTask(tsk.Id);
                if (ts != null)
                {
                    log.Debug("Skipping already active transition {0}", ts.CorrelationId);
                    continue;
                }
                if (!CanEnableTransition(tsk.Id))
                {
                    log.Info("Transition {0} cannot be enabled, skipping", tsk.Id);
                    continue;
                }
                log.Info("Enabling transition {0}", tsk.Id);
                EnableTransition(tsk.Id);
            }
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
            return false;
        }

        private bool CanEnableTransition(string taskId)
        {
            List<string> enablingPlaces;
            return CanEnableTransition(taskId, out enablingPlaces);
        }

        private void EnableTransition(string taskId)
        {
            Debug.Assert(CanEnableTransition(taskId));

        }


        #region IProcessTransitionCallback Members

        /// <summary>
        /// Transition started.
        /// Remove tokens from its input places.
        /// </summary>
        /// <param name="correlationId"></param>
        public void TransitionStarted(string correlationId)
        {
            ActiveTransition at = GetActiveInstanceOfTask(correlationId);
            if (at == null) throw new Exception("Invalid correlation Id");
            if (at.Status != TransitionStatus.ENABLED) throw new Exception("Invalid transition status");
            Task tsk = Definition.GetTask(at.TaskId);
            List<string> enablingPlaces;
            bool b = CanEnableTransition(at.TaskId, out enablingPlaces);
            if (!b) throw new Exception("Should never happen!");
            foreach (string plid in enablingPlaces)
            {
                ConsumeToken(plid, correlationId);
            }
            at.AllocatedPlaces = enablingPlaces;
            at.Status = TransitionStatus.STARTED;
        }

        public void TransitionCompleted(string correlationId)
        {
            ActiveTransition at = GetActiveInstanceOfTask(correlationId);
            if (at == null) throw new Exception("Invalid correlation Id");
            if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
            Task tsk = Definition.GetTask(at.TaskId);
            if (at.Status == TransitionStatus.ENABLED)
            {
                TransitionStarted(correlationId);
            }
            Debug.Assert(at.Status == TransitionStatus.STARTED);
#warning TODO
            //get task data here
            //and produce output tokens
        }

        /// <summary>
        /// Consume a token from specified place.
        /// Cancels all other transitions if they no longer can be enabled.
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="correlationId"></param>
        private void ConsumeToken(string placeId, string consumingCorrelationId)
        {
            RemoveToken(placeId);
            Place pl = Definition.GetPlace(placeId);
            foreach (Task tsk in pl.NodesOut)
            {
                ActiveTransition at = GetActiveInstanceOfTask(tsk.Id);
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
                    log.Info("Transition {0} no longer can be enabled. Cancelling.", at.CorrelationId);
                    CancelTransition(at.CorrelationId);
                }
            }
        }

        /// <summary>
        /// Cancel active transition. 
        /// Returns tokens to input places if the transition has been STARTED.
        /// </summary>
        /// <param name="correlationId"></param>
        private void CancelTransition(string correlationId)
        {
            ActiveTransition at = GetActiveTransition(correlationId);
            if (at == null) throw new Exception("Invalid correlation Id");
            if (at.Status != TransitionStatus.ENABLED && at.Status != TransitionStatus.STARTED) throw new Exception("Invalid transition status");
            if (at.Status == TransitionStatus.STARTED)
            {
                //return tokens to input places
                foreach (string plid in at.AllocatedPlaces)
                {
                    AddToken(plid);
                }
            }
            at.Cancel();
            at.Status = TransitionStatus.CANCELLED;
        }

        

        #endregion
    }
}

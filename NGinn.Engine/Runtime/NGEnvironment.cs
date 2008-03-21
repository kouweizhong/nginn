using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib;
using NGinn.Engine.Services;
using NGinn.Engine.Services.Dao;
using NLog;

namespace NGinn.Engine.Runtime
{
    public class NGEnvironment : INGEnvironment
    {
        private Spring.Context.IApplicationContext _appCtx;
        private static Logger log = LogManager.GetCurrentClassLogger();
        private IProcessDefinitionRepository _definitionRepository;
        private IProcessInstanceRepository _instanceRepository;
        private INGDataStore _dataStore;
        private IProcessInstanceLockManager _lockManager;
        public NGEnvironment()
        {
            //_appCtx = Spring.Context.Support.ContextRegistry.GetContext();
            //_definitionRepository = (IProcessDefinitionRepository) _appCtx.GetObject("ProcessDefinitionRepository");
            //_instanceRepository = (IProcessInstanceRepository)_appCtx.GetObject("ProcessInstanceRepository");
        }

        public IProcessDefinitionRepository DefinitionRepository
        {
            get { return _definitionRepository; }
            set { _definitionRepository = value; }
        }

        public IProcessInstanceRepository InstanceRepository
        {
            get { return _instanceRepository; }
            set { _instanceRepository = value; }
        }

        public INGDataStore DataStore
        {
            get { return _dataStore; }
            set { _dataStore = value; }
        }

        public IProcessInstanceLockManager LockManager
        {
            get { return _lockManager; }
            set { _lockManager = value; }
        }

        void RunSingleStep()
        {
            //1. select process instance to kick
            //2. kick the instance
            //3. commit changes

        }

        #region INGEnvironment Members

        public string StartProcessInstance(string definitionId, IDictionary<string, object> inputVariables)
        {
            ProcessDefinition pd = _definitionRepository.GetProcessDefinition(definitionId);
            if (pd == null) throw new Exception("Process definition not found");
            using (INGDataSession ds = DataStore.OpenSession())
            {
                ProcessInstance pi = InstanceRepository.InitializeNewProcessInstance(definitionId, ds);
                log.Info("Created new process instance for process {0}.{1}: {2}", pd.Name, pd.Version, pi.InstanceId);
                pi.ProcessDefinitionId = definitionId;
                pi.ProcessVariables = inputVariables;
                Token tok = pi.CreateNewStartToken();
                pi.AddToken(tok);
                InstanceRepository.UpdateProcessInstance(pi, ds);
                ds.Commit();
                return pi.InstanceId;
            }
            
        }

        #endregion

        /// <summary>
        /// This function returns list of 'kickable' process instances.
        /// If the list returned is empty, it means that there are no processes ready for kick.
        /// Otherwise, the list contains some process ID's, but it will return just some ids, not necessarily
        /// all of them. The algorithm of selecting/ordering the ids is undefined - maybe random.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetKickableProcesses()
        {
            return InstanceRepository.SelectProcessesWithReadyTokens();
        }

        public void KickProcess(string instanceId)
        {
            log.Info("Kicking process {0}", instanceId);

            if (!LockManager.TryAcquireLock(instanceId))
            {
                log.Info("Failed to lock process {0}. Ignoring");
                return;
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(instanceId, ds);
                    Token tok = pi.SelectReadyTokenForProcessing();
                    if (tok == null)
                    {
                        log.Info("Process {0} has no ready tokens.", instanceId);
                        return;
                    }
                    KickToken(tok, pi, ds);
                    ds.Commit();
                }
            }
            finally
            {
                LockManager.ReleaseLock(instanceId);
            }
            
        }

        private void KickToken(Token tok, ProcessInstance inst, INGDataSession ds)
        {
            log.Info("Kicking token {0} for process {1}", tok.TokenId, tok.ProcessInstanceId);
            ProcessDefinition pd = DefinitionRepository.GetProcessDefinition(inst.ProcessDefinitionId);
            if (tok.Status != TokenStatus.READY) throw new Exception("Invalid token status");
            if (tok.Mode == TokenMode.DEAD)
            {
                KickDeadToken(tok, inst, ds);
                return;
            }
            Place pl = pd.GetPlace(tok.PlaceId);
            foreach (Task tsk in pl.NodesOut)
            {
                
            }
        }

        private void KickDeadToken(Token tok, ProcessInstance inst, INGDataSession ds)
        {
            throw new NotImplementedException();
        }
    }
}

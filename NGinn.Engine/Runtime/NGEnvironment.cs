using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib;
using NGinn.Engine.Services;
using NGinn.Engine.Services.Dao;
using NGinn.Lib.Interfaces.MessageBus;
using NLog;
using System.Xml;
using System.IO;
using System.Xml.Schema;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.Worklist;
using NGinn.Lib.Data;
using NGinn.Engine.Runtime.Tasks;

namespace NGinn.Engine.Runtime
{
    public class NGEnvironment : INGEnvironment, INGEnvironmentProcessCommunication, INGEnvironmentContext
    {
        private Spring.Context.IApplicationContext _appCtx;
        private static Logger log = LogManager.GetCurrentClassLogger();
        private IProcessDefinitionRepository _definitionRepository;
        private IProcessInstanceRepository _instanceRepository;
        private IWorkListService _worklistService;
        private INGDataStore _dataStore;
        private IProcessInstanceLockManager _lockManager;
        private IDictionary<string, object> _envVariables = new Dictionary<string, object>();
        private IMessageBus _mbus;
        private bool _markErrors = true;

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

        public IWorkListService WorklistService
        {
            get { return _worklistService; }
            set { _worklistService = value; }
        }

        public IMessageBus MessageBus
        {
            get { return _mbus; }
            set {
                lock (this)
                {
                    if (_mbus != null)
                        _mbus.UnsubscribeObject(this);
                    _mbus = value;
                    _mbus.SubscribeObject(this);
                }
            }
        }

        public IDictionary<string, object> EnvironmentVariables
        {
            get { return _envVariables; }
        }

        /// <summary>
        /// true - when KickProcess fails, mark the failing process
        /// with 'Error' status, so it will not be selected for processing
        /// next time.
        /// false - do not mark failing process with 'Error' status, 
        /// its persisted version will remain unchanged
        /// </summary>
        public bool MarkInstanceErrors
        {
            get { return _markErrors; }
            set { _markErrors = value; }
        }

        /// <summary>
        /// Set environment variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetEnvVariable(string name, object value)
        {
            lock (_envVariables)
            {
                if (_envVariables.ContainsKey(name))
                    _envVariables.Remove(name);
                _envVariables.Add(name, value);
            }
        }

        public object GetEnvVariable(string name)
        {
            lock (_envVariables)
            {
                if (_envVariables.ContainsKey(name))
                    return _envVariables[name];
                return null;
            }
        }

        void RunSingleStep()
        {
            //1. select process instance to kick
            //2. kick the instance
            //3. commit changes

        }

        #region INGEnvironment Members

        public string StartProcessInstance(string definitionId, string inputXml, string processCorrelationId)
        {
            try
            {
                ProcessDefinition pd = _definitionRepository.GetProcessDefinition(definitionId);
                if (pd == null) throw new ApplicationException("Process definition not found: " + definitionId);
                pd.ValidateProcessInputXml(inputXml);
                
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.InitializeNewProcessInstance(definitionId, ds);
                    pi.Environment = this;
                    pi.CorrelationId = processCorrelationId;
                    pi.Activate();

                    log.Info("Created new process instance for process {0}.{1}: {2}", pd.Name, pd.Version, pi.InstanceId);
                    pi.ProcessDefinitionId = definitionId;
                    pi.SetProcessInputData(inputXml);
                    Token tok = pi.CreateNewStartToken();
                    pi.AddToken(tok);
                    pi.Passivate();
                    InstanceRepository.UpdateProcessInstance(pi, ds);
                    ds.Commit();
                    return pi.InstanceId;
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Error starting process: {0}", ex));
                throw;
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

            if (!LockManager.TryAcquireLock(instanceId, 0))
            {
                log.Info("Failed to lock process {0}. Ignoring");
                return;
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(instanceId, ds);
                    bool error = false;
                    try
                    {
                        pi.Environment = this;
                        pi.Activate();
                        log.Info("Original: {0}", pi.ToString());
                        pi.Kick();
                        log.Info("Modified: {0}", pi.ToString());
                        pi.Passivate();
                    }
                    catch(Exception ex)
                    {
                        //error moving process forward. Mark it for retry ....
                        log.Error("Error updating process {0} : {1}", instanceId, ex);
                        if (MarkInstanceErrors)
                        {
                            error = true;
                            InstanceRepository.SetProcessInstanceErrorStatus(instanceId, ex.ToString(), ds);
                        }
                        else
                        {
                            throw ex;
                        }
                    }

                    if (!error)
                    {
                        InstanceRepository.UpdateProcessInstance(pi, ds);
                    }
                    ds.Commit();
                }
            }
            finally
            {
                LockManager.ReleaseLock(instanceId);
            }
            
        }

       


        public void ProcessTaskCompleted(TaskCompletionInfo info)
        {
            log.Info("Task completed in process {0}. Id: {1}", info.ProcessInstance, info.CorrelationId);
            if (!LockManager.TryAcquireLock(info.ProcessInstance, 30000))
            {
                log.Info("Failed to obtain lock on process instance {0}", info.ProcessInstance);
                throw new Exception("Failed to lock process instance");
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(info.ProcessInstance, ds);
                    pi.Environment = this;
                    pi.Activate();
                    log.Info("Original: {0}", pi.ToString());
                    pi.TransitionCompleted(info);
                    log.Info("Modified: {0}", pi.ToString());
                    pi.Passivate();
                    InstanceRepository.UpdateProcessInstance(pi, ds);
                    ds.Commit();
                }
            }
            finally
            {
                LockManager.ReleaseLock(info.ProcessInstance);
            }
        }


        

        public void ProcessTaskSelectedForProcessing(string instanceId, string correlationId)
        {
            log.Info("Task selected in process {0}. Id: {1}", instanceId, correlationId);
            if (!LockManager.TryAcquireLock(instanceId, 30000))
            {
                log.Info("Failed to obtain lock on process instance {0}", instanceId);
                throw new Exception("Failed to lock process instance");
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(instanceId, ds);
                    pi.Environment = this;
                    pi.Activate();
                    log.Info("Original: {0}", pi.ToString());
                    pi.TransitionSelected(correlationId);
                    log.Info("Modified: {0}", pi.ToString());
                    pi.Passivate();
                    InstanceRepository.UpdateProcessInstance(pi, ds);
                    ds.Commit();
                }
            }
            finally
            {
                LockManager.ReleaseLock(instanceId);
            }
        }


        public string GetProcessInstanceData(string instanceId)
        {
            if (!LockManager.TryAcquireLock(instanceId, 30000))
            {
                log.Info("Failed to obtain lock on process instance {0}", instanceId);
                throw new ApplicationException("Failed to lock process instance");
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(instanceId, ds);
                    pi.Environment = this;
                    pi.Activate();
                    IDataObject dob = pi.GetProcessVariablesContainer();
                    return dob.ToXmlString(pi.Definition.Name);
                }
            }
            finally
            {
                LockManager.ReleaseLock(instanceId);
            }
        }

        public string GetTaskInstanceXml(string correlationId)
        {
            string instanceId = correlationId.Substring(0, correlationId.IndexOf('.'));
            if (!LockManager.TryAcquireLock(instanceId, 30000))
            {
                log.Info("Failed to obtain lock on process instance {0}", instanceId);
                throw new ApplicationException("Failed to lock process instance");
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(instanceId, ds);
                    pi.Environment = this;
                    pi.Activate();
                    IDataObject dob = pi.GetTaskData(correlationId);
                    return dob.ToXmlString("data");
                }
            }
            finally
            {
                LockManager.ReleaseLock(instanceId);
            }
        }


        /// <summary>
        /// Handle internal process events coming from the message bus
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [MessageBusSubscriber(typeof(InternalTransitionEvent), "*")]
        protected object HandleInternalTransitionEvent(string topic, string sender, object msg)
        {
            InternalTransitionEvent ite = (InternalTransitionEvent)msg;
            if (!LockManager.TryAcquireLock(ite.ProcessInstanceId, 30000))
            {
                log.Info("Failed to obtain lock on process instance {0}", ite.ProcessInstanceId);
                throw new ApplicationException("Failed to lock process instance");
            }
            try
            {
                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.GetProcessInstance(ite.ProcessInstanceId, ds);
                    pi.Environment = this;
                    pi.Activate();
                    pi.DispatchInternalTransitionEvent(ite);
                    pi.Passivate();
                    InstanceRepository.UpdateProcessInstance(pi, ds);
                    ds.Commit();
                }
            }
            finally
            {
                LockManager.ReleaseLock(ite.ProcessInstanceId);
            }
            return null;
        }

        [MessageBusSubscriber(typeof(ProcessFinished), "ProcessInstance.*")]
        protected object HandleProcessFinished(string topic, string sender, object msg)
        {
            ProcessFinished pf = (ProcessFinished)msg;
            log.Debug("Process finished: {0}", pf.InstanceId);
            if (pf.CorrelationId == null || pf.CorrelationId.Length == 0)
            {
                return null;
            }
            if (!SubprocessTaskActive.IsSubprocessCorrelationId(pf.CorrelationId))
            {
                return null;
            }
            string taskCorrId = SubprocessTaskActive.GetTaskCorrelationIdFromProcess(pf.CorrelationId);
            log.Debug("Subprocess completed, task correlation id: {0}", taskCorrId);
            TaskCompletionInfo tci = new TaskCompletionInfo();
            tci.CorrelationId = taskCorrId;
            tci.ProcessInstance = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(taskCorrId);
            tci.CompletedDate = pf.TimeStamp;
            tci.ResultXml = null; //todo

            this.ProcessTaskCompleted(tci);
            return null;
        }
    }
}

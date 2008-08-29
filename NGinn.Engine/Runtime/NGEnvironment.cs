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
        private IResourceManager _resMgr;
        private IActiveTaskFactory _activeTaskFactory = new ActiveTaskFactory();
        private ITaskCorrelationIdResolver _resolver;

        private bool _markErrors = true;

        public NGEnvironment()
        {
        }

        /// <summary>
        /// Environment's process definition repository
        /// </summary>
        public IProcessDefinitionRepository DefinitionRepository
        {
            get { return _definitionRepository; }
            set { _definitionRepository = value; }
        }

        /// <summary>
        /// Factory used for creating new active tasks
        /// </summary>
        public IActiveTaskFactory ActiveTaskFactory
        {
            get { return _activeTaskFactory; }
            set { _activeTaskFactory = value; }
        }

        /// <summary>
        /// Process instance repository implementation
        /// </summary>
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

        public ITaskCorrelationIdResolver CorrelationIdResolver
        {
            get { return _resolver; }
            set { _resolver = value; }
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

        public IResourceManager ResourceManager
        {
            get { return _resMgr; }
            set { _resMgr = value; }
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

        #region INGEnvironment Members



        public string StartProcessInstance(string definitionId, IDataObject inputData, string userId, string correlationId)
        {
            try
            {
                ProcessDefinition pd = _definitionRepository.GetProcessDefinition(definitionId);
                if (pd == null) throw new ApplicationException("Process definition not found: " + definitionId);
                StructDef sd = pd.GetProcessInputDataSchema();
                inputData.Validate(sd);

                using (INGDataSession ds = DataStore.OpenSession())
                {
                    ProcessInstance pi = InstanceRepository.InitializeNewProcessInstance(definitionId, ds);
                    pi.Environment = this;
                    pi.CorrelationId = correlationId;
                    pi.Activate();

                    log.Info("Created new process instance for process {0}.{1}: {2}", pd.Name, pd.Version, pi.InstanceId);
                    pi.ProcessDefinitionId = definitionId;
                    pi.SetProcessInputData(inputData);
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
            TaskCompletedNotification tcn = new TaskCompletedNotification();
            tcn.CorrelationId = info.CorrelationId;
            tcn.ProcessInstanceId = info.ProcessInstance;
            XmlReader xr = XmlReader.Create(new StringReader(info.ResultXml));
            xr.MoveToContent();
            tcn.TaskData = DataObject.ParseXmlElement(xr);
            this.MessageBus.Notify("NGEnvironment", "NGinn.ProcessTaskCompleted", tcn, false);
        }


        

        public void ProcessTaskSelectedForProcessing(string instanceId, string correlationId)
        {
            TransitionSelectedNotification ev = new TransitionSelectedNotification();
            ev.CorrelationId = correlationId;
            ev.ProcessInstanceId = instanceId;
            MessageBus.Notify("NGEnvironment", "NGinn.TransitionSelected", ev, false);
        }

        public DataObject GetProcessOutputData(string instanceId)
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
                    if (pi.Status != ProcessStatus.Finished)
                        throw new ApplicationException("Invalid process status");
                    
                    DataObject dob = pi.GetProcessOutputData();
                    return dob;
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



        public DataObject GetTaskData(string correlationId)
        {
            string instanceId = correlationId.Substring(0, correlationId.IndexOf('.'));
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
                    return new DataObject(pi.GetTaskData(correlationId));
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
            string parentProcessId = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(taskCorrId);
            log.Debug("Subprocess completed, task correlation id: {0}. Notifying parent process {1}", taskCorrId, parentProcessId);

            SubprocessCompleted sc = new SubprocessCompleted();
            sc.SubprocessInstanceId = pf.InstanceId;
            sc.ProcessInstanceId = parentProcessId;
            sc.CorrelationId = taskCorrId;

            MessageBus.Notify("NGEnvironment", "SubprocessCompleted", sc, true);
            return null;
        }

        #region INGEnvironment Members


        public void CancelProcessInstance(string instanceId)
        {
            log.Info("Cancelling process {0}", instanceId);
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
                    pi.CancelProcessInstance();
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

        


        public void DispatchProcessMessage(string messageCorrelationId, DataObject messageBody)
        {
            string taskCorrelationId = CorrelationIdResolver.GetCorrelationId(messageCorrelationId);
            if (taskCorrelationId == null) throw new ApplicationException("Did not find any process waiting for message with message CorrelationId=" + messageCorrelationId);
            //ok, task found - so notify it about the message
            ReceiveMessageTaskEvent te = new ReceiveMessageTaskEvent();
            te.CorrelationId = taskCorrelationId;
            te.ProcessInstanceId = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(taskCorrelationId);
            te.MessageCorrelationId = messageCorrelationId;
            te.MessageData = messageBody;
            MessageBus.Notify("NGEnvironment", "ReceiveMessageTaskEvent", te, false);
        }

        public void ReportTaskFinished(string correlationId, DataObject updatedTaskData, string userId)
        {
            TaskCompletedNotification tn = new TaskCompletedNotification();
            tn.CompletedBy = userId;
            tn.CorrelationId = correlationId;
            tn.ProcessInstanceId = ProcessInstance.ProcessInstanceIdFromTaskCorrelationId(correlationId);
            tn.TaskData = updatedTaskData;
            MessageBus.Notify("NGEnvironment", "ReportTaskFinished", tn, false);
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib;
using NGinn.Engine.Services;
using NGinn.Engine.Services.Dao;
using NGinn.MessageBus;
using NLog;
using System.Xml;
using System.IO;
using System.Xml.Schema;

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
            set { _mbus = value; }
        }

        public IDictionary<string, object> EnvironmentVariables
        {
            get { return _envVariables; }
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

        public string StartProcessInstance(string definitionId, string inputXml)
        {
            ProcessDefinition pd = _definitionRepository.GetProcessDefinition(definitionId);
            if (pd == null) throw new Exception("Process definition not found");
            ValidateProcessInputData(pd, inputXml);
            
            using (INGDataSession ds = DataStore.OpenSession())
            {
                ProcessInstance pi = InstanceRepository.InitializeNewProcessInstance(definitionId, ds);
                pi.Environment = this;
                pi.Activate();

                //foreach (VariableDef vd in pd.InputVariables)
                //{
                    /* object val = null;
                    if (tmp.ContainsKey(vd.Name))
                    {
                        val = Convert.ChangeType(inputVariables[vd.Name], vd.VariableType);
                        tmp.Remove(vd.Name);
                    }
                    else
                    {
                        if (vd.VariableDir == VariableDef.Direction.In || vd.VariableDir == VariableDef.Direction.InOut)
                        {
                            if (vd.VariableUsage == VariableDef.Usage.Required)
                            {
                                throw new Exception("Missing required input variable: " + vd.Name);
                            }
                        }
                    }
                    pi.ProcessVariables[vd.Name] = val;
                    */
                //}

                log.Info("Created new process instance for process {0}.{1}: {2}", pd.Name, pd.Version, pi.InstanceId);
                pi.ProcessDefinitionId = definitionId;
                
                Token tok = pi.CreateNewStartToken();
                pi.AddToken(tok);
                pi.Passivate();
                InstanceRepository.UpdateProcessInstance(pi, ds);
                ds.Commit();
                return pi.InstanceId;
            }
            
        }

        #endregion

        private void ValidateProcessInputData(ProcessDefinition pd, string inputXml)
        {
            string schemaXml = pd.GenerateInputSchema();
            StringReader sr = new StringReader(inputXml);
            XmlSchema xs = XmlSchema.Read(new StringReader(schemaXml), new ValidationEventHandler(schema_ValidationEventHandler));
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas = new XmlSchemaSet();
            rs.Schemas.Add(xs);
            rs.ValidationEventHandler += new ValidationEventHandler(rs_ValidationEventHandler);
            XmlReader xr = XmlReader.Create(sr, rs);
            while (xr.Read())
            {
            }
        }

        void schema_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                throw new Exception("Input schema validation error: " + e.Message);
            }
            else
            {
                log.Info("Schema validation warning: {0}", e.Message);
            }
        }

        void rs_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                throw new Exception("Input xml validation error: " + e.Message);
            }
            else
            {
                log.Info("Input xml validation warning: {0}", e.Message);
            }
        }

        

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
                    pi.Environment = this;
                    pi.Activate();
                    log.Info("Original: {0}", pi.ToString());
                    Token tok = pi.SelectReadyTokenForProcessing();
                    if (tok == null)
                    {
                        log.Info("Process {0} has no ready tokens.", instanceId);
                        return;
                    }
                    KickToken(tok, pi, ds);
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
            else
            {
                inst.KickReadyToken(tok);
            }
        }

        private void KickDeadToken(Token tok, ProcessInstance inst, INGDataSession ds)
        {
            throw new NotImplementedException();
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

        
    }
}

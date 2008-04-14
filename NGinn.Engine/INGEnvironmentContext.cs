using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Engine.Services.Dao;
using NGinn.MessageBus;

namespace NGinn.Engine
{
    public interface INGEnvironmentContext
    {
        IProcessDefinitionRepository DefinitionRepository
        {
            get;
        }

        IProcessInstanceRepository InstanceRepository
        {
            get;
        }
         

        INGDataStore DataStore
        {
            get; 
        }
        
        IProcessInstanceLockManager LockManager
        {
            get; 
        }
         
        IWorkListService WorklistService
        {
            get;
        }

        IMessageBus MessageBus
        {
            get;
        }
    }
}

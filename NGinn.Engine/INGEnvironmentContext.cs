using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Engine.Services.Dao;
using NGinn.Lib.Interfaces;
using NGinn.Lib.Interfaces.Worklist;
using NGinn.Lib.Interfaces.MessageBus;
using System.Collections;

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

        IActiveTaskFactory ActiveTaskFactory
        {
            get;
        }

        ITaskCorrelationIdResolver CorrelationIdResolver
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

        IResourceManager ResourceManager
        {
            get;
        }

        IProcessScriptManager ScriptManager
        {
            get;
        }

        IDictionary EnvironmentVariables { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine;
using NGinn.Engine.Runtime.Scripting;
using NGinn.Lib.Schema;
using System.IO;
using NLog;
using Rhino.DSL;
using NGinn.Engine.Services;
using Antlr.StringTemplate;

namespace NGinn.Engine.Runtime.Scripting.BooScript
{
    public class BooProcessScriptManager : IProcessScriptManager
    {
        private Logger log = LogManager.GetCurrentClassLogger();
        private DslFactory _fact;

        private string _baseDir;
        /// <summary>
        /// Base directory where generated script files are stored
        /// </summary>
        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }

        private string _templatesDir;
        public string TemplateDir
        {
            get { return _templatesDir; }
            set { _templatesDir = value; }
        }

        private DslFactory GetDslFactory()
        {
            lock (this)
            {
                if (_fact == null)
                {
                    _fact = new DslFactory();
                    _fact.BaseDirectory = BaseDirectory;
                    NGinnDslEngine eng1 = new NGinnDslEngine();
                    eng1.BaseType = typeof(BooTaskScriptBase);
                    _fact.Register(typeof(BooTaskScriptBase), eng1);
                }
                return _fact;
            }
        }

        private void GenerateProcessScripts(ProcessDefinition pd)
        {
            
        }

        private void GenerateTaskCode(ProcessDefinition pd, Task tsk, TextWriter output)
        {

        }

       

        #region IProcessScriptManager Members

        public void ProcessDefinitionUpdated(ProcessDefinition pd)
        {
            log.Info("Process definition updated: {0}", pd.DefinitionId);
            
        }

        public void PackageDefinitionUpdated(PackageDefinition pd)
        {
            throw new NotImplementedException();
        }

        public IProcessScript GetProcessScript(ProcessDefinition pd)
        {
            throw new NotImplementedException();
        }

        public ITaskScript GetTaskScript(ProcessDefinition pd, string taskId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

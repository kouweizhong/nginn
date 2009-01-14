using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Runtime;
using NGinn.Engine.Services;
using NLog;
using SN = ScriptNET;
using System.IO;

namespace NGinn.Engine.Runtime.Scripting.ScriptNet
{
    
    public class ScriptNetProcessScriptManager : IProcessScriptManager
    {
        private Logger log = LogManager.GetCurrentClassLogger();

        private string _runtimeConfigPath;

        public string RuntimeConfigPath
        {
            get { return _runtimeConfigPath; }
            set { _runtimeConfigPath = value; }
        }

        public ScriptNetProcessScriptManager()
        {
            
        }

        class ProcessScriptCache
        {
            public string DefinitionId;
            public Dictionary<string, SN.Script> ScriptsCache = new Dictionary<string, SN.Script>();
        }

        private bool _inited = false;
        public void Init()
        {
            string s = RuntimeConfigPath;
            if (s == null || s.Length == 0)
            {
                s = typeof(ScriptNetProcessScriptManager).Assembly.Location;
                s = Path.Combine(Path.GetDirectoryName(s), "Runtime/Scripting/ScriptNet/RuntimeConfig.xml");
            }
            log.Info("Initializing script runtime from {0}", s);
            using (FileStream fs = new FileStream(s, FileMode.Open))
            {
                SN.Runtime.RuntimeHost.Initialize(fs);
            }
            _inited = true;
        }

        private Dictionary<string, ProcessScriptCache> _cache = new Dictionary<string, ProcessScriptCache>();

        #region IProcessScriptManager Members

        public void ProcessDefinitionUpdated(NGinn.Lib.Schema.ProcessDefinition pd)
        {
            log.Info("Process {0} updated, removing cached scripts", pd.DefinitionId);
            _cache.Remove(pd.DefinitionId);
        }

        public void PackageDefinitionUpdated(NGinn.Lib.Schema.PackageDefinition pd)
        {
            log.Info("Package {0} updated, removing cached scripts", pd.PackageName);
            _cache.Clear();
        }

        public IProcessScript GetProcessScript(NGinn.Lib.Schema.ProcessDefinition pd)
        {
            return new ProcessScript(this, pd);
        }

        public ITaskScript GetTaskScript(NGinn.Lib.Schema.ProcessDefinition pd, string taskId)
        {
            return new TaskScript(this, pd.GetTask(taskId));
            return null;
        }

        #endregion

        internal SN.Script GetCachedProcessScript(string definitionId, string scriptId)
        {
            ProcessScriptCache pc;
            if (!_cache.TryGetValue(definitionId, out pc)) return null;
            SN.Script scr;
            if (!pc.ScriptsCache.TryGetValue(scriptId, out scr)) return null;
            return scr;
        }

        internal void SetCachedProcessScript(string definitionId, string scriptId, SN.Script scr)
        {
            ProcessScriptCache pc;
            lock (this)
            {
                if (!_cache.TryGetValue(definitionId, out pc))
                {
                    pc = new ProcessScriptCache();
                    pc.DefinitionId = definitionId;
                    _cache[definitionId] = pc;
                }
            }
            lock (pc.ScriptsCache)
            {
                if (pc.ScriptsCache.ContainsKey(scriptId))
                    pc.ScriptsCache.Remove(scriptId);
                pc.ScriptsCache[scriptId] = scr;
            }
        }
    }
}

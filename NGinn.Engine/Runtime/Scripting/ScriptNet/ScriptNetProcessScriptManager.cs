using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Runtime;
using NGinn.Engine.Services;
using NLog;
using ScriptNET;

namespace NGinn.Engine.Runtime.Scripting.ScriptNet
{
    
    public class ScriptNetProcessScriptManager : IProcessScriptManager
    {
        class ProcessScriptCache
        {
            public string DefinitionId;
            public Dictionary<string, Script> ScriptsCache = new Dictionary<string, Script>();
        }

        private Dictionary<string, ProcessScriptCache> _cache = new Dictionary<string, ProcessScriptCache>();

        #region IProcessScriptManager Members

        public void ProcessDefinitionUpdated(NGinn.Lib.Schema.ProcessDefinition pd)
        {
            _cache.Remove(pd.DefinitionId);
        }

        public void PackageDefinitionUpdated(NGinn.Lib.Schema.PackageDefinition pd)
        {
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

        internal Script GetCachedProcessScript(string definitionId, string scriptId)
        {
            ProcessScriptCache pc;
            if (!_cache.TryGetValue(definitionId, out pc)) return null;
            Script scr;
            if (!pc.ScriptsCache.TryGetValue(scriptId, out scr)) return null;
            return scr;
        }

        internal void SetCachedProcessScript(string definitionId, string scriptId, Script scr)
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

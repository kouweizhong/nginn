using System;
using System.Collections.Generic;
using System.Text;
using Spring.Objects.Factory.Config;
using NLog;
using System.IO;

namespace NGinn.Lib.Util
{
    /// <summary>
    /// Configuration info provider.
    /// It looks for configuration (properties) file in 
    /// application base (bin) directory and above.
    /// Additionally, provides some configurations properties 
    /// such as ng.basedir, ng.machinename
    /// </summary>
    public class DefaultConfigProvider : IVariableSource
    {
        private Dictionary<string, string> _variables = new Dictionary<string, string>();
        private static Logger log = LogManager.GetCurrentClassLogger();
        private PropertyFileVariableSource _props = null; 
        private string _configFile = "nginn.properties";
        private string _defaultPrefix = "ng.";

        public DefaultConfigProvider()
        {
            SetInternal("basedir", Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
            SetInternal("machinename", Environment.MachineName);
            LoadConfigFile();
        }

        public string ResolveVariable(string name)
        {
            string val;
            if (!_variables.TryGetValue(name, out val))
                return _props == null ? null : _props.ResolveVariable(name);
            return val;
        }

        public void Set(string name, string value)
        {
            log.Debug("Setting: {0}={1}", name, value);
            _variables[name] = value;
        }

        private void SetInternal(string name, string value)
        {
            Set(DefaultPrefix + name, value);
        }

        public string ConfigFile
        {
            get { return _configFile; }
            set { _configFile = value; }
        }

        public string DefaultPrefix
        {
            get { return _defaultPrefix; }
            set { _defaultPrefix = value; }
        }

        protected void LoadConfigFile()
        {
            string dir = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            DirectoryInfo di = new DirectoryInfo(dir);
            bool found = false;
            while (di != null && di.Parent != null && di != di.Parent)
            {
                string fn = Path.Combine(di.FullName, ConfigFile);
                log.Debug("Looking for config in {0}", fn);
                if (File.Exists(fn))
                {
                    log.Debug("Loading config from {0}", fn);
                    _props = new PropertyFileVariableSource();
                    _props.Location = new Spring.Core.IO.FileSystemResource(fn);
                    SetInternal("configfile", fn);
                    SetInternal("configdir", di.FullName);
                    found = true;
                    break;
                }
                else
                {
                    di = di.Parent;
                }
            }
            if (!found)
            {
                log.Warn("Config file not found: {0}", ConfigFile);
            }
        }
    }
}

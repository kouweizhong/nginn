using System;
using System.Collections.Generic;
using System.Text;
using Sooda.Config;
using NLog;

namespace NGinn.Engine.Dao
{
    public class SpringConfigProvider : ISoodaConfigProvider
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public string GetString(string key)
        {
            log.Debug("Getting config for {0}", key);
            throw new Exception("The method or operation is not implemented.");
        }
    }
}

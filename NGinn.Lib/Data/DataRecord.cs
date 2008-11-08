using System;
using System.Collections.Generic;
using System.Text;
using MutantFramework;
using NLog;

namespace NGinn.Lib.Data
{
    public class DataRecord : DataMutant, IMutant
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public object Get(string Name, object[] index)
        {
            log.Info("GET: {0}", Name);
            return base.Get(Name, index);
        }
    }
}

using System;
using System.Collections.Specialized;
using System.Text;

namespace NGinn.Lib.Config
{
    public class SimpleConfig : IConfig
    {
        private NameValueCollection _config = new NameValueCollection();

        #region IConfig Members

        public string GetString(string name)
        {
            return GetString(name, null);
        }

        public string GetString(string name, string defVal)
        {
            string ret = _config[name];
            if (ret == null) ret = defVal;
            return ret;
        }

        public string SubstValue(string expr)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        public string this[string keyName]
        {
            get { return GetString(keyName); }
            set {
                _config[keyName] = value;
            }
        }
    }
}

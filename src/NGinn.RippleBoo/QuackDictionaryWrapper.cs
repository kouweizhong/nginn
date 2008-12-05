using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Boo.Lang;

namespace NGinn.RippleBoo
{
    public class QuackWrapper : IQuackFu
    {
        private IDictionary _dic;

        public QuackWrapper(IDictionary dic)
        {
            _dic = dic;
        }

        public QuackWrapper(IDictionary<string, object> dic)
        {
            _dic = new Hashtable();
            if (dic != null)
                foreach (string key in dic.Keys) _dic[key] = dic[key];
        }

        #region IQuackFu Members

        public object QuackGet(string name, object[] parameters)
        {
            object v = _dic[name];
            if (parameters != null && parameters.Length > 0) throw new Exception("Indexers not supported");
            if (v is IDictionary)
            {
                return new QuackWrapper(v as IDictionary);
            }
            return v;
        }

        public object QuackInvoke(string name, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object QuackSet(string name, object[] parameters, object value)
        {
            if (parameters != null && parameters.Length > 0) throw new Exception("Indexers not supported");
            _dic[name] = value;
            return value;
        }

        #endregion
    }
}

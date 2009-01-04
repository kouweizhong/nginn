using System;
using System.Collections.Generic;
using System.Text;
using Boo.Lang;
using Spring.Context;

namespace NGinn.Utilities
{
    /// <summary>
    /// Spring application context wrapper for accessing
    /// the context from Boo DSLs
    /// </summary>
    public class AppContextQuackFu : IApplicationContextAware, IQuackFu
    {
        public AppContextQuackFu()
        {
        }

        public AppContextQuackFu(IApplicationContext ctx)
        {
            _ctx = ctx;
        }

        private IApplicationContext _ctx;
        #region IApplicationContextAware Members

        public IApplicationContext ApplicationContext
        {
            get { return _ctx; }
            set { _ctx = value; }
        }

        #endregion

        #region IQuackFu Members

        public object QuackGet(string name, object[] parameters)
        {
            return _ctx.GetObject(name, parameters);
        }

        public object QuackInvoke(string name, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object QuackSet(string name, object[] parameters, object value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services.Dao;
using Sooda;

namespace NGinn.Engine.Dao
{
    public class NGDataStore : INGDataStore
    {
        #region INGDataStore Members

        public INGDataSession OpenSession()
        {
            return new SoodaSession();
        }

        #endregion
    }

    public class SoodaSession : INGDataSession
    {
        private SoodaTransaction _st;

        public SoodaTransaction Transaction
        {
            get { return _st; }
        }

        public SoodaSession()
        {
            _st = new SoodaTransaction();
        }
        #region INGDataSession Members

        public void Commit()
        {
            _st.Commit();
        }

        public void Rollback()
        {
            _st.Rollback();
        }

        public void Dispose()
        {
            _st.Dispose();
        }
        #endregion
    }
}

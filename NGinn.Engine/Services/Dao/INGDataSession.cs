using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services.Dao
{
    public interface INGDataSession : IDisposable
    {
        void Commit();
        void Rollback();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services.Dao
{
    public interface INGDataStore
    {
        INGDataSession OpenSession();
    }
}

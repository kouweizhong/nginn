using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces.Worklist
{
    public interface ITODOListDataProvider
    {
        string GetListDataXml(string dataQuery);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace XmlForms.Interfaces
{
    public interface IListDataProvider
    {
        string GetListData(string listName, string listQuery);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace XmlForms.Interfaces
{
    public interface IListInfoProvider
    {
        ListInfo GetListInfo(string listName);
    }
}

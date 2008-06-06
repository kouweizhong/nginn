using System;
using System.Collections.Generic;
using System.Text;
using XmlForms.Interfaces;

namespace XmlForms.Lists
{
    class DefaultListDataProvider : IListDataProvider
    {
        #region IListDataProvider Members

        public string GetListData(string listName, string listQuery)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

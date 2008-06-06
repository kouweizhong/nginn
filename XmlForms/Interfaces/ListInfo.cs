using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using NLog;
using System.Xml;

namespace XmlForms.Interfaces
{
    [Serializable]
    public class ListInfo
    {
        private static XmlSerializer _ser = new XmlSerializer(typeof(ListInfo), new Type[] { typeof(ListColumnInfo) });
            
        private string _listName;

        public string ListName
        {
            get { return _listName; }
            set { _listName = value; }
        }
        
        private string _keyField;

        public string KeyField
        {
            get { return _keyField; }
            set { _keyField = value; }
        }

        private string _recordClass;

        public string RecordClass
        {
            get { return _recordClass; }
            set { _recordClass = value; }
        }


        private ListColumnInfo[] _columns;

        public ListColumnInfo[] Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }

        private string _dataProvider;

        public string DataProvider
        {
            get { return _dataProvider; }
            set { _dataProvider = value; }
        }

        public void ToXml(XmlWriter xw)
        {
            _ser.Serialize(xw, this);
        }

        public static ListInfo FromXml(XmlReader xr)
        {
            return (ListInfo)_ser.Deserialize(xr);
        }


    }

    [Serializable]
    public class ListColumnInfo
    {
        private string _header;

        public string Header
        {
            get { return _header; }
            set { _header = value; }
        }
        private bool _sortable  = true;

        public bool Sortable
        {
            get { return _sortable; }
            set { _sortable = value; }
        }
        private string _dataField;

        public string DataField
        {
            get { return _dataField; }
            set { _dataField = value; }
        }
        private string _dataType;

        public string DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }
        private bool _visible = true;

        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }
        private string _width;

        public string Width
        {
            get { return _width; }
            set { _width = value; }
        }

        private string _dataExpression;

        public string DataExpression
        {
            get { return _dataExpression; }
            set { _dataExpression = value; }
        }
    }
}

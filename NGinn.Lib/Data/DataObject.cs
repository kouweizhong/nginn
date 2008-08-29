using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Wintellect.PowerCollections;
using System.Collections;
using NLog;
using MutantFramework;

namespace NGinn.Lib.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDataObject : IDictionary<string, object>, IDictionary
    {
        /// <summary>
        /// Get value of specified field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index">Optional indexer. Can be null.</param>
        /// <returns></returns>
        object Get(string name, object[] index);
        /// <summary>
        /// Set value of specified field
        /// </summary>
        /// <param name="name">Field name</param>
        /// <param name="index">Optional indexer. Can be null.</param>
        /// <param name="newValue">New field value</param>
        void Set(string name, object[] index, object newValue);
        /// <summary>
        /// Get expression value. Convenience method for accessing nested members.
        /// Expression example: field1.otherField[0].someData
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        object GetValue(string expression);
        /// <summary>
        /// Set expression value. Convenience method for modifying nested data
        /// Expression example: order.items[0].amount
        /// </summary>
        /// <param name="expression">Property to be set</param>
        /// <param name="value">new value for the property</param>
        void SetValue(string expression, object value);
        /// <summary>
        /// List of fields in the data object
        /// </summary>
        IList<string> FieldNames { get; }
        /// <summary>
        /// Convert data object to xml representation
        /// </summary>
        /// <param name="elementName">root element name</param>
        /// <param name="output"></param>
        void ToXml(string elementName, XmlWriter output);
        /// <summary>
        /// The same as ToXML, but returns Xml in a string
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        string ToXmlString(string elementName);

        /// <summary>
        /// Merge with another data object. Can append or replace matching fields - according to 'replace' parameter.
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="replace"></param>
        void Merge(IDictionary dic, bool replace);

        /// <summary>
        /// Validate record structure against given type definition
        /// </summary>
        /// <param name="recordType"></param>
        void Validate(StructDef recordType);

        /// <summary>
        /// Validate record structure
        /// </summary>
        void Validate();

        /// <summary>
        /// Return dataobject's structure definition
        /// </summary>
        /// <returns></returns>
        StructDef GetRecordType();
    }

    [Serializable]
    public class DataObject : DictionaryBase<string, object>, IDataObject
    {
        private List<string> _membersAdded = new List<string>();
        private Dictionary<string, object> _data = new Dictionary<string, object>();
        private static Logger log = LogManager.GetCurrentClassLogger();
        [NonSerialized]
        private StructDef _recType = null;

        public DataObject()
        {
        }

        public DataObject(IDictionary dic) 
        {
            if (dic == null) return;
            foreach (string key in dic.Keys)
            {
                this.Add(key, dic[key]);
            }
        }

        public DataObject(StructDef recType)
        {
            _recType = recType;
        }

        public override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            List<KeyValuePair<string, object>> lst = new List<KeyValuePair<string, object>>();
            foreach (string s in _membersAdded)
            {
                lst.Add(new KeyValuePair<string, object>(s, _data[s]));
            }
            return lst.GetEnumerator();
        }

        public IList<string> FieldNames
        {
            get
            {
                return _membersAdded;
            }
        }

        public override void Clear()
        {
            lock (this)
            {
                _data.Clear();
                _membersAdded.Clear();
            }
        }

        public override bool Remove(string key)
        {
            lock (this)
            {
                _data.Remove(key);
                return _membersAdded.Remove(key);
            }
        }

        public override bool TryGetValue(string key, out object value)
        {
            return _data.TryGetValue(key, out value);
        }

        public override int Count
        {
            get { return _data.Count; }
        }

        public override object this[string key]
        {
            get
            {
                if (!base.ContainsKey(key)) return null;
                return base[key];
            }
            set
            {
                lock (this)
                {
                    if (!ContainsKey(key))
                    {
                        _membersAdded.Add(key);
                    }
                    _data[key] = value;
                }
            }
        }
        
        public object Get(string name, object[] index)
        {
            object obj = this[name];

            if (index != null && index.Length > 0)
            {
                if (index.Length > 1) throw new Exception("Only single index is allowed");
                int idx = Convert.ToInt32(index[0]);
                if (obj is IList)
                {
                    IList col = (IList)obj;
                    return col[idx];
                }
                else
                {
                    if (idx == 0) return obj;
                    throw new Exception("Index outside array bounds");
                }
            }
            else return obj;
        }

        /// <summary>
        /// TODO:skonczyc
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <param name="newValue"></param>
        public void Set(string name, object[] index, object newValue)
        {
            if (index != null && index.Length > 0) throw new Exception("Indexed setter not implemented");
            if (_recType != null)
            {
                MemberDef md = _recType.GetMember(name);
                if (md == null) throw new ApplicationException(string.Format("Field {0}.{1} not declared"));
                TypeDef mType = _recType.ParentTypeSet.GetTypeDef(md.TypeName);
                if (md.IsArray)
                {
                    
                }

                if (mType is StructDef)
                {
                    
                }
                else if (mType is SimpleTypeDef)
                {
                    SimpleTypeDef std = (SimpleTypeDef) mType;
                    

                }
            }
            
            this[name] = newValue;
        }

        /// <summary>
        /// Get value of a data-binding expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public object GetValue(string expression)
        {
            int idx = expression.IndexOf('.');
            string propName = expression;
            if (idx >= 0)
            {
                propName = expression.Substring(0, idx);
            }
            int indexer = 0;
            bool hasIndexer = false;
            if (propName.EndsWith("]"))
            {
                int t = propName.LastIndexOf('[');
                if (t < 0) throw new Exception("Missing [");
                string sIdx = propName.Substring(t + 1, propName.Length - t - 2);
                if (!Int32.TryParse(sIdx, out indexer)) throw new Exception("Indexer invalid: " + sIdx);
                hasIndexer = true;
                propName = propName.Substring(0, t);
            }

            object[] index = hasIndexer ? new object[] { indexer } : null;
            object val = Get(propName, index);
            if (idx < 0)
            {
                return val; //this is the end...
            }
            else
            {
                string expression2 = expression.Substring(idx + 1);
                if (!(val is DataObject)) throw new Exception(string.Format("Cannot access {0} in expression {1} - not a DataObject", propName, expression));
                return ((DataObject)val).GetValue(expression2);
            }
        }

        public void SetValue(string expression, object newValue)
        {

        }

        public void ToXml(string elementName, XmlWriter output)
        {
            if (elementName != null) output.WriteStartElement(elementName);
            foreach (KeyValuePair<string, object> pair in this)
            {
                ToXml(pair.Value, pair.Key, output);
            }
            if (elementName != null) output.WriteEndElement();
        }

        private void ToXml(object val, string elementName, XmlWriter output)
        {
            if (val is DataObject)
            {
                ((DataObject)val).ToXml(elementName, output);
            }
            else if (val is System.Collections.ICollection)
            {
                System.Collections.ICollection col = (System.Collections.ICollection)val;
                foreach (object obj in col)
                {
                    ToXml(obj, elementName, output);
                }
            }
            else
            {
                output.WriteElementString(elementName, val.ToString());
            }
        }

        public string ToXmlString(string elementName)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            XmlWriter xw = XmlWriter.Create(sb, ws);
            ToXml(elementName, xw);
            xw.Flush();
            return sb.ToString();
        }

        public void Merge(IDictionary dic, bool replace)
        {
            foreach (string key in dic.Keys)
            {
                
            }
        }

        /// <summary>
        /// XML reading:
        /// attributes -> string entries
        /// simple elements (without content) -> string entries
        /// array of elements (more than 1 element with the same name) -> array of object (ICollection)
        /// complex elements (with sub-elements)-> DataObject
        /// </summary>
        /// <param name="xr"></param>
        /// <returns></returns>
        public static DataObject ParseXmlElement(XmlReader xr)
        {
            object obj = DeserializeXmlElement(xr);
            if (obj == null) return new DataObject(); //empty data object
            if (!(obj is DataObject)) throw new ApplicationException("XML does not contain data object");
            return (DataObject) obj;
        }

        /// <summary>
        /// Invariant: start: we're standing at start element
        ///            end: we're standing at end element (the same as start)
        /// </summary>
        /// <param name="xr"></param>
        /// <returns></returns>
        private static object DeserializeXmlElement(XmlReader xr)
        {
            if (xr.NodeType != XmlNodeType.Element) throw new Exception();
            if (xr.IsEmptyElement)
                return null;
            string txtval = null;
            DataObject dob = null;
            string nodeName = xr.Name;
            if (xr.HasAttributes)
            {
                log.Warn("Ignoring XML attributes");
            }

            while (xr.Read())
            {
                log.Debug("Node: {0} ({1})",xr.Name, xr.NodeType.ToString());
                switch (xr.NodeType)
                {
                    case XmlNodeType.EndElement:
                        if (dob != null)
                            return dob;
                        else
                            return txtval;
                        break;
                    case XmlNodeType.Whitespace:
                        continue; //skip whitespace
                        break;
                    case XmlNodeType.Text:
                        if (dob != null) throw new ApplicationException("Element cannot have mixed content: " + nodeName);
                        txtval = xr.Value;
                        break;
                    case XmlNodeType.Attribute:
                        log.Debug("Ignoring attribute {0}={1}", xr.Name, xr.Value);
                        continue;
                        break;
                    case XmlNodeType.Element:
                        string elName = xr.Name;
                        object elValue = DeserializeXmlElement(xr);
                        if (txtval != null) throw new ApplicationException("Element cannot have mixed content: " + nodeName);
                        if (dob == null) dob = new DataObject();
                        if (dob.ContainsKey(elName))
                        {
                            object curVal = dob[elName];
                            if (!(curVal is IList))
                            {
                                IList col = new List<object>();
                                col.Add(curVal);
                                col.Add(elValue);
                                dob[elName] = col;
                            }
                            else
                            {
                                IList col = (IList)curVal;
                                col.Add(elValue);
                            }
                        }
                        else
                        {
                            dob[elName] = elValue;
                        }
                        break;
                    default:
                        throw new Exception(string.Format("Unexpected node type: {0} ({1})", nodeName, xr.NodeType));
                }
            }
            return null;
            
        }

        public StructDef GetRecordType()
        {
            return _recType;
        }

        public static DataObject ParseXml(string xml)
        {
            XmlReader xr = XmlReader.Create(new System.IO.StringReader(xml));
            xr.MoveToContent();
            return ParseXmlElement(xr);
        }

        public void Validate(StructDef recordType)
        {
            Dictionary<string, MemberDef> d = new Dictionary<string, MemberDef>();
            foreach (MemberDef md in recordType.Members)
            {
                d[md.Name] = md;
                object v = this[md.Name];
                TypeDef mType = recordType.ParentTypeSet.GetTypeDef(md.TypeName);
                if (v == null && md.IsRequired) throw new Exception("Missing required value: " + md.Name);
                if (md.IsArray)
                {
                    if (v is IList)
                    {
                        IList arr = (IList)v;
                        if (arr.Count == 0 && md.IsRequired) throw new Exception("At least one value is required in field " + md.Name);
                        foreach (object v2 in ((IList)v))
                        {
                            if (recordType.ParentTypeSet.IsBasicType(md.TypeName))
                            {
                                ValidateSingleObject(md.Name, v2, mType);
                            }
                            else
                            {
                                StructDef tdef = recordType.ParentTypeSet.GetStructType(md.TypeName);
                                if (tdef == null) throw new Exception(); //should never happen
                                if (!(v2 is IDataObject))
                                    throw new Exception(string.Format("Field {0} content invalid. Expected record of type {1}", md.Name, md.TypeName));
                                else
                                {
                                    IDataObject d2 = (IDataObject)v2;
                                    d2.Validate(tdef);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (recordType.ParentTypeSet.IsBasicType(md.TypeName) ||
                        recordType.ParentTypeSet.IsEnumType(md.TypeName))
                    {
                        ValidateSingleObject(md.Name, v, mType);
                    }
                    else
                    {
                        StructDef tdef = recordType.ParentTypeSet.GetStructType(md.TypeName);
                        if (tdef == null) throw new Exception(); //should never happen
                        if (!(v is IDataObject))
                            throw new Exception(string.Format("Field {0} content invalid. Expected record of type {1}", md.Name, md.TypeName));
                        else
                        {
                            IDataObject d2 = (IDataObject)v;
                            d2.Validate(tdef);
                        }
                    }
                }
            }
            foreach (string s in this.FieldNames)
            {
                object v = Get(s, null);
                if (!d.ContainsKey(s) && v != null) throw new Exception(string.Format("Field {0} is not declared", s));
            }
        }

        protected void ValidateSingleObject(string name, object v, TypeDef td)
        {
            if (td is StructDef)
            {
                if (!(v is IDataObject)) throw new ApplicationException(string.Format("Field {0} should be a record of type {1}", name, td.Name));
                IDataObject dob = (IDataObject)v;
                dob.Validate((StructDef)td);
            }
            else if (td is SimpleTypeDef)
            {
                SimpleTypeDef std = (SimpleTypeDef)td;
            }
            else if (td is EnumDef)
            {
                EnumDef ed = (EnumDef)td;
                if (!ed.EnumValues.Contains(v))
                {
                    throw new ApplicationException(string.Format("Value {0} not defined in enum {1}", v, ed.Name));
                }
            }
            else throw new Exception();
        }

        public void Validate()
        {
            if (this.GetRecordType() == null) throw new ApplicationException("Cannot validate record - record type not defined");
            Validate(GetRecordType());
        }
    }
}

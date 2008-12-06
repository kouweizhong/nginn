using System;
using System.Collections.Generic;
using System.Text;
using MutantFramework;
using NLog;

namespace NGinn.Lib.Data
{
    /// <summary>
    /// Mutantic wrapper of DataObject
    /// </summary>
    public class DOBMutant : IMutant
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private IDataObject _dob;
        #region Mutant Members

        public DOBMutant(IDataObject dob)
        {
            _dob = dob;
        }

        public DOBMutant()
        {
            _dob = new DataObject();
        }

        public void AssignTo(object o)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void CaptureFields(IMutant mt)
        {
            string[] lst = mt.GetMutantFields();
            foreach (string fld in lst)
            {
                object v = mt.Get(fld, null);
                Set(fld, v, null);
            }
        }

        public string FriendlyString()
        {
            return _dob.ToXmlString("root");
        }

        public object Get(string Name, object[] index)
        {
            log.Debug("Get: {0}", Name);
            object v = _dob.Get(Name, index);
            if (v is IDataObject)
            {
                v = new DOBMutant((IDataObject)v);
            }
            return v;
        }

        public string[] GetMutantFields()
        {
            IList<string> fs = _dob.FieldNames;
            string[] ret = new string[fs.Count];
            fs.CopyTo(ret, 0);
            return ret;
        }

        public object Invoke(string[] FuncName, object[] param)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object Invoke(string FuncName, object[] param)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Mutate(object Victim)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object Resolve()
        {
            return _dob;
        }

        public void Set(string Name, object value, object[] index)
        {
            log.Debug("SET: {0}={1}", Name, value);
            object v = value;
            if (v is DOBMutant)
            {
                v = ((DOBMutant)v)._dob;
            }
            else if (v is IMutant)
            {
                v = DataMutantConverter.ToDataObject((IMutant)v);
            }
            _dob.Set(Name, index, v);
        }

        #endregion
    }
}

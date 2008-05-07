using System;
using System.Collections.Generic;
using System.Text;
using MutantFramework;
using NLog;

namespace NGinn.Lib.Data
{
    public class DOBMutant : Mutant
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

        public void CaptureFields(Mutant mt)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string FriendlyString()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object Get(string Name, object[] index)
        {
            log.Debug("Get: {0}[{1}]", Name, index[0]);
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
            throw new Exception("The method or operation is not implemented.");
        }

        public void Set(string Name, object value, object[] index)
        {
            log.Debug("SET: {0}[{1}]={2}", Name, index, value);
            object v = value;
            if (value is Mutant)
            {
                v = DataMutantConverter.ToDataObject((Mutant)v);
            }
            _dob.Set(Name, index, value);
        }

        #endregion
    }
}

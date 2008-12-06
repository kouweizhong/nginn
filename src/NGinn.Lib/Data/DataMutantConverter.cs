using System;
using System.Collections.Generic;
using System.Text;
using MutantFramework;

namespace NGinn.Lib.Data
{
    public class DataMutantConverter
    {
        public static IMutant ToMutant(DataObject dob)
        {
            DataMutant dm = new DataMutant(dob, DataMutantBehavior.CreateMutantField);
            foreach (string name in dob.Keys)
            {
                object val = dob[name];
                if (val is DataObject)
                {
                    val = ToMutant((DataObject)val);
                }
                dm.Set(name, val, null);
            }
            return dm;
        }

        public static DataObject ToDataObject(IMutant mut)
        {
            DataObject dob = new DataObject();
            string[] fields = mut.GetMutantFields();
            foreach (string field in fields)
            {
                dob[field] = mut.Get(field, null);
            }
            return dob;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XmlProc
{
    public enum FormFieldMode
    {
        Text,
        Textarea,
        DateTime,
        DateOnly,
        Select,
        Reffield,
        Checkbox,
        Integer
    }

    public enum FieldAccess
    {
        ReadOnly = 0,
        ReadWrite = 1,
        Required = 2
    }

    class FormFieldInfo
    {
        public string Name;
        public FormFieldMode Mode = FormFieldMode.Text;
        public Type DataType;
        public FieldAccess Access;
        public string ValidationRegex;
        public string DataSource;
        public string DisplayValue;
        public string Value;
        public IList<string> FieldOptions;
    }

    class DefaultDataBinder
    {
        private object _root;

        public DefaultDataBinder(object root)
        {
            _root = root;
        }

        public object GetExprValue(string fldName)
        {
            object curRoot = _root;
            Type curType = _root.GetType();

            string[] arr = fldName.Split('.');
            for (int i = 0; i < arr.Length; i++)
            {
                object v;
                bool found = false;
                PropertyInfo pi = curType.GetProperty(arr[i]);
                if (pi != null)
                {
                    v = pi.GetValue(curRoot, null);
                    curRoot = v;
                    curType = pi.PropertyType;
                    found = true;
                }
                if (!found)
                {
                    FieldInfo fi = curType.GetField(arr[i]);
                    if (fi != null)
                    {
                        v = fi.GetValue(curRoot);
                        curRoot = v;
                        curType = fi.FieldType;
                        found = true;
                    }
                }

                if (!found) throw new Exception(string.Format("{0} not found in {1}", arr[i], curType.Name));
                if (curRoot == null)
                    break;
            }
            return curRoot;
        }

        public void SetFieldValue(string fldName, object v)
        {
        }

        public FormFieldInfo GetFieldInfo(string fldName)
        {
            FormFieldInfo fi = new FormFieldInfo();
            fi.Name = fldName;
            object v = GetExprValue(fldName);
            fi.Value = Convert.ToString(v);
            fi.DataType = v == null ? null : v.GetType();
            fi.Access = FieldAccess.ReadWrite;
            fi.Mode = FormFieldMode.Text;

            return fi;
        }


        /// <summary>
        /// sooda://Klasa/id
        /// ref://Typ/id
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static object GetObjectByRef(string reference)
        {
            if (reference.StartsWith(_soodaRef))
            {
                string t = reference.Substring(_soodaRef.Length);
                string[] arr = t.Split('*');
                if (arr.Length != 2) throw new Exception("Invalid sooda reference: " + reference);
                return Sooda.SoodaTransaction.ActiveTransaction.GetObject(arr[0], arr[1]);
            }
            else if (reference.StartsWith(_refRef))
            {
                string t = reference.Substring(_refRef.Length);
                string[] arr = t.Split('*');
                if (arr.Length != 2) throw new Exception("Invalid object reference: " + reference);
                Type tp = Type.GetType(arr[0]);
                if (tp == null) throw new Exception("Type not found: " + reference);
                MethodInfo mi = tp.GetMethod("GetRef", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
                if (mi == null) throw new Exception("static GetRef not found in type " + tp.Name);
                ParameterInfo[] pi = mi.GetParameters();
                if (pi.Length != 1) throw new Exception("GetRef should have 1 argument");
                return mi.Invoke(tp, new object[] {Convert.ChangeType(arr[1], pi[0].ParameterType)});
            }
            else throw new Exception("Invalid object reference: " + reference);
        }

        public const string _soodaRef = "sooda*";
        public const string _refRef = "ref*";
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinn.Lib.Schema;

namespace NGinn.Lib.Data
{
    
    [Serializable]
    public class TypeSet
    {
        public static readonly SimpleTypeDef TYPE_STRING = new SimpleTypeDef("string", typeof(string));
        public static readonly SimpleTypeDef TYPE_INT = new SimpleTypeDef("int", typeof(Int32));
        public static readonly SimpleTypeDef TYPE_DOUBLE = new SimpleTypeDef("double", typeof(double));
        public static readonly SimpleTypeDef TYPE_DATE = new SimpleTypeDef("date", typeof(DateTime));
        public static readonly SimpleTypeDef TYPE_DATETIME = new SimpleTypeDef("datetime", typeof(DateTime));
        
        private Dictionary<string, TypeDef> _types;

        static TypeSet()
        {
            
        }

        public TypeSet()
        {
            _types = new Dictionary<string, TypeDef>();
            _types.Add(TYPE_STRING.Name, TYPE_STRING);
            _types.Add(TYPE_INT.Name, TYPE_INT);
            _types.Add(TYPE_DOUBLE.Name, TYPE_DOUBLE);
            _types.Add(TYPE_DATETIME.Name, TYPE_DATETIME);
            _types.Add(TYPE_DATE.Name, TYPE_DATE);
        }

        public bool IsBasicType(string typeName)
        {
            TypeDef td = GetTypeDef(typeName);
            if (td is SimpleTypeDef) return true;
            return false;
        }

        public bool IsTypeDefined(string typeName)
        {
            return _types.ContainsKey(typeName);
        }

        public TypeDef GetTypeDef(string name)
        {
            TypeDef td;
            if (!_types.TryGetValue(name, out td)) return null;
            return td;
        }

        public StructDef GetStructType(string name)
        {
            return GetTypeDef(name) as StructDef;
        }

        public void AddType(TypeDef sd)
        {
            List<TypeDef> l = new List<TypeDef>();
            l.Add(sd);
            AddTypes(l);
        }

        public void AddTypes(ICollection<TypeDef> types)
        {
            ValidationCtx ctx = new ValidationCtx();
            foreach (StructDef sd in types)
            {
                if (IsTypeDefined(sd.Name)) throw new ApplicationException("Type already defined: " + sd.Name);
                ctx.NewTypes.Add(sd.Name, sd);
            }
            foreach (TypeDef sd in types)
            {
                ValidateTypeDef(sd, ctx);
            }
            foreach (TypeDef sd in types)
            {
                sd.ParentTypeSet = this;
                _types.Add(sd.Name, sd);
            }
        }

        
        private class ValidationCtx
        {
            public Dictionary<string, TypeDef> NewTypes = new Dictionary<string, TypeDef>();
            public Dictionary<string, TypeDef> ValidatedTypes = new Dictionary<string, TypeDef>();
        }

        private void ValidateTypeDef(TypeDef td, ValidationCtx ctx)
        {
            if (IsTypeDefined(td.Name)) return;
            if (ctx.ValidatedTypes.ContainsKey(td.Name)) return;
            ctx.ValidatedTypes.Add(td.Name, td);
            if (td is SimpleTypeDef)
            {
                return;
            }
            else if (td is StructDef)
            {
                StructDef sd = (StructDef)td;
                foreach (MemberDef md in sd.Members)
                {
                    if (!IsTypeDefined(md.TypeName))
                    {
                        if (!ctx.NewTypes.ContainsKey(md.TypeName))
                        {
                            throw new ApplicationException(string.Format("Member type ({0}) not defined for {1}.{2}", md.TypeName, sd.Name, md.Name));
                        }
                        TypeDef td2 = ctx.NewTypes[md.TypeName];
                        ValidateTypeDef(td2, ctx);
                    }
                }
            }
            else throw new Exception();
        }

        public ICollection<string> TypeNames
        {
            get
            {
                return _types.Keys;
            }
        }

        /// <summary>
        /// Generate XML schema for the whole type set
        /// </summary>
        /// <param name="xw"></param>
        public void WriteXmlSchema(XmlWriter xw)
        {
            foreach(string tdName in TypeNames)
            {
                TypeDef td = GetTypeDef(tdName);
                td.WriteXmlSchemaType(xw);
            }
        }

    }
}

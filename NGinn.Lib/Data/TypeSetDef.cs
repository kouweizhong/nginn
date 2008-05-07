using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinn.Lib.Data
{
    
    [Serializable]
    public class TypeSet
    {
        public static readonly string TYPE_STRING = "string";
        public static readonly string TYPE_INT = "int";
        public static readonly string TYPE_DOUBLE = "double";
        public static readonly string TYPE_DATE = "date";
        public static readonly string TYPE_DATETIME = "datetime";
        private static IDictionary<string, Type> _basicTypes;
        private Dictionary<string, StructDef> _structs;

        static TypeSet()
        {
            _basicTypes = new Dictionary<string, Type>();
            _basicTypes[TYPE_STRING] = typeof(string);
            _basicTypes[TYPE_INT] = typeof(int);
            _basicTypes[TYPE_DOUBLE] = typeof(double);
            _basicTypes[TYPE_DATE] = typeof(DateTime);
            _basicTypes[TYPE_DATETIME] = typeof(DateTime);
        }

        public TypeSet()
        {
            _structs = new Dictionary<string, StructDef>();
        }

        public static bool IsBasicType(string typeName)
        {
            return _basicTypes.ContainsKey(typeName);
        }

        public bool IsTypeDefined(string typeName)
        {
            return _structs.ContainsKey(typeName) || _basicTypes.ContainsKey(typeName);
        }

        public StructDef GetStructType(string name)
        {
            StructDef sd;
            if (!_structs.TryGetValue(name, out sd)) return null;
            return sd;
        }

        public void AddStructType(StructDef sd)
        {
            List<StructDef> l = new List<StructDef>();
            l.Add(sd);
            AddStructTypes(l);
        }

        public void AddStructTypes(ICollection<StructDef> types)
        {
            ValidationCtx ctx = new ValidationCtx();
            foreach (StructDef sd in types)
            {
                if (IsTypeDefined(sd.Name)) throw new ApplicationException("Type already defined: " + sd.Name);
                ctx.NewTypes.Add(sd.Name, sd);
            }
            foreach (StructDef sd in types)
            {
                ValidateTypeDef(sd, ctx);
            }
            foreach (StructDef sd in types)
            {
                _structs.Add(sd.Name, sd);
            }
        }

        
        private class ValidationCtx
        {
            public Dictionary<string, StructDef> NewTypes = new Dictionary<string, StructDef>();
            public Dictionary<string, StructDef> ValidatedTypes = new Dictionary<string, StructDef>();
        }

        private void ValidateTypeDef(StructDef sd, ValidationCtx ctx)
        {
            if (IsTypeDefined(sd.Name)) return;
            if (ctx.ValidatedTypes.ContainsKey(sd.Name)) return;
            ctx.ValidatedTypes.Add(sd.Name, sd);
            foreach(MemberDef md in sd.Members)
            {
                if (!IsTypeDefined(md.TypeName))
                {
                    if (!ctx.NewTypes.ContainsKey(md.TypeName))
                    {
                        throw new ApplicationException(string.Format("Member type ({0}) not defined for {1}.{2}", md.TypeName, sd.Name, md.Name));
                    }
                    StructDef sd2 = ctx.NewTypes[md.TypeName];
                    ValidateTypeDef(sd2, ctx);
                }
            }
        }

        /// <summary>
        /// Generate XML schema for the whole type set
        /// </summary>
        /// <param name="xw"></param>
        public void WriteXmlSchema(XmlWriter xw)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generate XML schema for given type
        /// </summary>
        /// <param name="sd"></param>
        /// <param name="includeDependencies">Include schema for types used in sd struct</param>
        /// <param name="xw"></param>
        public void WriteXmlSchema(StructDef sd, bool includeDependencies, XmlWriter xw)
        {
            throw new NotImplementedException();
        }
    }
}

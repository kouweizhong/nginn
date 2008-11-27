using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.DSL;
using NLog;
using System.Reflection;

namespace NGinn.RippleBoo
{
    public class RippleBooDslEngine : DslEngine
    {
        private Type _baseType = typeof(RuleSetBase);
        private Logger log = LogManager.GetCurrentClassLogger();

        public Type BaseType
        {
            get { return _baseType; }
            set { _baseType = value; }
        }

        private string[] _namespaces = new string[] {};

        public string[] Namespaces
        {
            get { return _namespaces; }
            set { _namespaces = value; }
        }

        private Assembly[] _refAssemblies;

        public Assembly[] ReferencedAssemblies
        {
            get { return _refAssemblies; }
            set { _refAssemblies = value; }
        }

        protected override void CustomizeCompiler(Boo.Lang.Compiler.BooCompiler compiler, Boo.Lang.Compiler.CompilerPipeline pipeline, string[] urls)
        {
            compiler.Parameters.Ducky = true;
            Assembly[] asms = _refAssemblies;
            if (asms == null) asms = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (Assembly asm in asms)
            {
                try
                {
                    string loc = asm.Location;
                    if (!compiler.Parameters.References.Contains(asm)) compiler.Parameters.References.Add(asm);
                }
                catch (Exception) { log.Debug("Error adding assembly dependency: {0}", asm.FullName); }
            }

            pipeline.Insert(1, new ImplicitBaseClassCompilerStep(
                _baseType, "Prepare", _namespaces));
        }
    }
}

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

        private bool _referAllLoaded = false;
        /// <summary>
        /// Set to true if all assemblies in current appdomain should be referenced
        /// </summary>
        public bool ReferAllLoadedAssemblies
        {
            get { return _referAllLoaded; }
            set { _referAllLoaded = value; }
        }

        protected override void CustomizeCompiler(Boo.Lang.Compiler.BooCompiler compiler, Boo.Lang.Compiler.CompilerPipeline pipeline, string[] urls)
        {
            compiler.Parameters.Ducky = true;
            List<Assembly> asmss = new List<Assembly>();
            if (ReferAllLoadedAssemblies)
            {
                asmss.AddRange(AppDomain.CurrentDomain.GetAssemblies());
            }
            if (ReferencedAssemblies != null) asmss.AddRange(ReferencedAssemblies);
            
            foreach (Assembly asm in asmss)
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
            pipeline.Insert(2, new AutoReferenceFilesCompilerStep());
        }
    }
}

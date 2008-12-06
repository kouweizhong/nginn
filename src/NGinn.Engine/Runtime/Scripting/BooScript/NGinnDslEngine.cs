using System;
using System.Collections.Generic;
using System.Text;
using Rhino.DSL;
using NLog;
using Boo.Lang.Compiler;
using Boo.Lang;

namespace NGinn.Engine.Runtime.Scripting.BooScript
{
    public class NGinnDslEngine : DslEngine
    {
        private Type _baseType = typeof(BooTaskScriptBase);

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

        protected override void CustomizeCompiler(Boo.Lang.Compiler.BooCompiler compiler, Boo.Lang.Compiler.CompilerPipeline pipeline, string[] urls)
        {
            
            compiler.Parameters.Ducky = true;
            
            pipeline.Insert(1, new ImplicitBaseClassCompilerStep(
                _baseType, "Prepare", _namespaces));
        }
    }
}

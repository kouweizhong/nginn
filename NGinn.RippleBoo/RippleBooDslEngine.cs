using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.DSL;
using NLog;

namespace NGinn.RippleBoo
{
    public class RippleBooDslEngine : DslEngine
    {
        private Type _baseType = typeof(RuleSetBase);

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

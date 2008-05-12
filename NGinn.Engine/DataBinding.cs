using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using ScriptNET;

namespace NGinn.Engine
{
    public class DataBinding
    {
        public static void ExecuteDataBinding(IDataObject target, IList<VariableBinding> bindings, IScriptContext ctx)
        {
            foreach (VariableBinding vb in bindings)
            {
                if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
                {
                    target[vb.VariableName] = ctx.GetItem(vb.BindingExpression, ContextItem.Variable);
                }
                else if (vb.BindingType == VariableBinding.VarBindingType.Expr)
                {
                    string expr = vb.BindingExpression.Trim();
                    if (!expr.EndsWith(";")) expr += ";";
                    target[vb.VariableName] = Script.RunCode(expr, ctx);
                }
                else throw new Exception("Binding type not supported");
            }
        }
    }
}

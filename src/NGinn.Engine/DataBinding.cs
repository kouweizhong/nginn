using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;
using NGinn.Engine.Services;

namespace NGinn.Engine
{
    public class DataBinding
    {
        /// <summary>
        /// TODO remove
        /// </summary>
        /// <param name="target"></param>
        /// <param name="bindings"></param>
        /// <param name="scr"></param>
        public static void ExecuteTaskInputDataBinding(IDataObject target, IList<VariableBinding> bindings, ITaskScript scr)
        {
            foreach (VariableBinding vb in bindings)
            {
                if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
                {
                    target[vb.VariableName] = scr.SourceData[vb.BindingExpression];
                }
                else if (vb.BindingType == VariableBinding.VarBindingType.Expr)
                {
                    target[vb.VariableName] = scr.EvalInputVariableBinding(vb.VariableName);
                }
                else throw new Exception("Binding type not supported");
            }
        }

        public static void ExecuteTaskOutputDataBinding(IDataObject target, IList<VariableBinding> bindings, ITaskScript scr)
        {
            foreach (VariableBinding vb in bindings)
            {
                if (vb.BindingType == VariableBinding.VarBindingType.CopyVar)
                {
                    target[vb.VariableName] = scr.SourceData[vb.BindingExpression];
                }
                else if (vb.BindingType == VariableBinding.VarBindingType.Expr)
                {
                    target[vb.VariableName] = scr.EvalOutputVariableBinding(vb.VariableName);
                }
                else throw new Exception("Binding type not supported");
            }
        }
        
    }
}

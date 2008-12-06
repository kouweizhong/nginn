using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using NLog;

namespace NGinn.Engine.Runtime.Scripting.BooScript
{
    public class BooProcessScriptBase : IProcessScript
    {
        private IDataObject _data;
        public NGinn.Lib.Data.IDataObject ProcessData
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        private ProcessInstance _pi;
        public ProcessInstance Instance
        {
            get { return _pi; }
            set { _pi = value; }
        }

        private INGEnvironmentContext _envCtx;
        public INGEnvironmentContext EnvironmentContext
        {
            get { return _envCtx; }
            set { _envCtx = value; }
        }

        public object GetDefaultVariableValue(string variableName)
        {
            return null;
        }

        public object EvaluateFlowInputCondition(Flow fl)
        {
            throw new NotImplementedException();
        }
    }
}

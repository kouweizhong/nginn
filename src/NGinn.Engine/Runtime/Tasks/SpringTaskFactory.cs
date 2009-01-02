using System;
using System.Collections;
using System.Text;
using NGinn.Lib.Schema;
using NLog;
using NGinn.Engine.Services;
using Spring.Context;

namespace NGinn.Engine.Runtime.Tasks
{
    public class SpringTaskFactory : IActiveTaskFactory, IApplicationContextAware
    {
        private IApplicationContext _ctx;
        private Logger log = LogManager.GetCurrentClassLogger();
        private IDictionary _taskFactories = new Hashtable();
        private IActiveTaskFactory _defaultFact = new ActiveTaskFactory();

        #region IActiveTaskFactory Members

        public IActiveTask CreateActiveTask(Task tsk)
        {
            string objType = tsk.ImplementationFactory;
            object obj = _taskFactories[objType];
            if (obj == null)
            {
                if (_ctx.ContainsObject(objType))
                {
                    IActiveTask atsk = (IActiveTask)_ctx.GetObject(objType, new object[] { tsk });
                    if (atsk == null) throw new Exception(string.Format("Failed to create active task for '{0}' ({1})", objType, tsk));
                    return atsk;
                }
                return _defaultFact.CreateActiveTask(tsk);
            }
            else if (obj is string)
            {
                objType = obj as string;
                IActiveTask atsk = (IActiveTask)_ctx.GetObject(objType, new object[] { tsk });
                if (atsk == null) throw new Exception(string.Format("Failed to create active task for '{0}' ({1})", objType, tsk));
                return atsk;
            }
            else if (obj is IActiveTaskFactory)
            {
                IActiveTaskFactory tf = (IActiveTaskFactory)obj;
                return tf.CreateActiveTask(tsk);
            }
            else throw new Exception("Invalid task factory config: " + objType);
        }

       
        #endregion

        #region IApplicationContextAware Members

        public IApplicationContext ApplicationContext
        {
            set { _ctx = value; }
        }

        public IDictionary TaskFactoryNames
        {
            get { return _taskFactories; }
            set { _taskFactories = value; }
        }
           
        #endregion
    }
}

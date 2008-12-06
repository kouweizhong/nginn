namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnEngineDaoStubs = NGinn.Engine.Dao.Stubs;
  
  
  public class ProcessInstanceDbXml : NGinnEngineDaoStubs.ProcessInstanceDbXml_Stub {
    
    public ProcessInstanceDbXml(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public ProcessInstanceDbXml(SoodaTransaction transaction) : 
        base(transaction) {
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
    }

    protected override void AfterFieldUpdate(string name, object oldVal, object newVal)
    {
        base.AfterFieldUpdate(name, oldVal, newVal);
        if (name != "LastModified")
        {
            LastModified = DateTime.Now;
        }
    }
    
    public ProcessInstanceDbXml() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

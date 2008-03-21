namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnEngineDaoStubs = NGinn.Engine.Dao.Stubs;
  
  
  public class ProcessStatus : NGinnEngineDaoStubs.ProcessStatus_Stub {
    
    public ProcessStatus(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public ProcessStatus(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public ProcessStatus() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

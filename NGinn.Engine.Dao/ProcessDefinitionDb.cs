namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinn.Engine.Dao.Stubs;
  
  
  public class ProcessDefinitionDb : NGinn.Engine.Dao.Stubs.ProcessDefinitionDb_Stub {
    
    public ProcessDefinitionDb(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public ProcessDefinitionDb(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public ProcessDefinitionDb() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

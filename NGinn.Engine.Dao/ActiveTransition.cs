namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnEngineDaoStubs = NGinn.Engine.Dao.Stubs;
  
  
  public class ActiveTransition : NGinnEngineDaoStubs.ActiveTransition_Stub {
    
    public ActiveTransition(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public ActiveTransition(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public ActiveTransition() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

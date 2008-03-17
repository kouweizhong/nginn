namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinn.Engine.Dao.Stubs;
  
  
  public class TokenDb : NGinn.Engine.Dao.Stubs.TokenDb_Stub {
    
    public TokenDb(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public TokenDb(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public TokenDb() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

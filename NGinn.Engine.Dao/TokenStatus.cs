namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinn.Engine.Dao.Stubs;
  
  
  public class TokenStatus : NGinn.Engine.Dao.Stubs.TokenStatus_Stub {
    
    public TokenStatus(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public TokenStatus(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public TokenStatus() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

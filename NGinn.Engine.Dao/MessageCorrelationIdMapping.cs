namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnEngineDaoStubs = NGinn.Engine.Dao.Stubs;
  
  
  public class MessageCorrelationIdMapping : NGinnEngineDaoStubs.MessageCorrelationIdMapping_Stub {
    
    public MessageCorrelationIdMapping(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public MessageCorrelationIdMapping(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public MessageCorrelationIdMapping() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

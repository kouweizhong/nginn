namespace NGinn.Engine.Dao {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinn.Engine.Dao.Stubs;
  
  
  public class ProcessInstanceDb : NGinn.Engine.Dao.Stubs.ProcessInstanceDb_Stub {

      

    public ProcessInstanceDb(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public ProcessInstanceDb(SoodaTransaction transaction) : 
        base(transaction) {
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
            
    }
    
    public ProcessInstanceDb() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

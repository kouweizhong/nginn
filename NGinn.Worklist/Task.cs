namespace NGinn.Worklist.BusinessObjects {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnWorklistBusinessObjectsStubs = NGinn.Worklist.BusinessObjects.Stubs;
  
  
  public class Task : NGinnWorklistBusinessObjectsStubs.Task_Stub {
    
    public Task(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public Task(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public Task() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

namespace NGinn.Worklist.BusinessObjects {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnWorklistBusinessObjectsStubs = NGinn.Worklist.BusinessObjects.Stubs;
  
  
  public class TaskStatus : NGinnWorklistBusinessObjectsStubs.TaskStatus_Stub {
    
    public TaskStatus(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public TaskStatus(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public TaskStatus() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

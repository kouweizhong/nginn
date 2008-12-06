namespace NGinn.Worklist.BusinessObjects {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnWorklistBusinessObjectsStubs = NGinn.Worklist.BusinessObjects.Stubs;
  
  
  public class Group : NGinnWorklistBusinessObjectsStubs.Group_Stub {
    
    public Group(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public Group(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public Group() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

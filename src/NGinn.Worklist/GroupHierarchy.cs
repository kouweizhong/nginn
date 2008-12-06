namespace NGinn.Worklist.BusinessObjects {
  using System;
  using System.Collections;
  using System.Diagnostics;
  using System.Data;
  using Sooda;
  using NGinnWorklistBusinessObjectsStubs = NGinn.Worklist.BusinessObjects.Stubs;
  
  
  public class GroupHierarchy : NGinnWorklistBusinessObjectsStubs.GroupHierarchy_Stub {
    
    public GroupHierarchy(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public GroupHierarchy(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public GroupHierarchy() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}

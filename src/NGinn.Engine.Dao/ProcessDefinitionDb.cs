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

      private void UpdateFullName()
      {
          FullName = string.Format("{0}.{1}", Name, Version);
      }

      protected override void AfterFieldUpdate_Name(object oldValue, object newValue)
      {
          base.AfterFieldUpdate_Name(oldValue, newValue);
          UpdateFullName();
      }

      protected override void AfterFieldUpdate_Version(object oldValue, object newValue)
      {
          base.AfterFieldUpdate_Version(oldValue, newValue);
          UpdateFullName();
      }
  }
}

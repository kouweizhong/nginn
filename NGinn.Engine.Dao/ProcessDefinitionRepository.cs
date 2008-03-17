using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using Sooda;
using NLog;
using NGinn.Lib.Schema;
using System.IO;
using NGinn.Engine.Dao.TypedQueries;

namespace NGinn.Engine.Dao
{
    public class ProcessDefinitionRepository : IProcessDefinitionRepository
    {
        private byte[] SerializePd(ProcessDefinition pd)
        {
            MemoryStream ms = new MemoryStream();
            //YAWN.Engine.Messaging.SerializationUtil.Serialize(pd, ms, YAWN.Engine.Messaging.MessageContentType.SerializedBinary);
            return ms.GetBuffer();
        }

        private ProcessDefinition DeserializePd(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            ms.Seek(0, SeekOrigin.Begin);
            //return (ProcessDefinition)YAWN.Engine.Messaging.SerializationUtil.Deserialize(ms, YAWN.Engine.Messaging.MessageContentType.SerializedBinary);
            return null;
        }


        public string InsertProcessDefinition(ProcessDefinition pd)
        {
            using (SoodaTransaction st = new SoodaTransaction())
            {
                ProcessDefinitionDb pdd = new ProcessDefinitionDb();
                pdd.Name = pd.Name;
                pdd.Version = pd.Version;
                pdd.FullName = string.Format("{0}.{1}", pd.Name, pd.Version);
                pdd.ProcessData = SerializePd(pd);
                st.Commit();
                return pdd.Id.ToString();
            }
        }

        public void UpdateProcessDefinition(ProcessDefinition pd)
        {
            using (SoodaTransaction st = new SoodaTransaction())
            {
                
                st.Commit();
            }
            throw new Exception("The method or operation is not implemented.");
        }

        public string GetProcessDefinitionId(string name, int version)
        {
            using (SoodaTransaction st = new SoodaTransaction())
            {
                ProcessDefinitionDbList lst = ProcessDefinitionDb.GetList(ProcessDefinitionDbField.Name == name && ProcessDefinitionDbField.Version == version);
                if (lst.Count == 0) return null;
                if (lst.Count > 1) throw new Exception("Not unique name/version");
                return lst[0].Id.ToString();
            }
        }

        public void DeleteProcessDefinition(string definitionId)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ProcessDefinition GetProcessDefinition(string name, int version)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ProcessDefinition GetProcessDefinition(string definitionId)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IList<string> GetProcessDefinitionNames()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IList<int> GetProcessDefinitionVersions(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IList<string> GetProcessDefinitionIds()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}

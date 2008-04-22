using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine.Services;
using NGinn.Engine.Dao.TypedQueries;
using Sooda;

namespace NGinn.Engine.Dao
{ 
    class ProcessInstanceRepository : IProcessInstanceRepository
    {

        #region IProcessInstanceRepository Members

        public ProcessInstance GetProcessInstance(string instanceId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession) ds;
            ProcessInstanceDbList dbl = ProcessInstanceDb.GetList(ss.Transaction, ProcessInstanceDbField.InstanceId == instanceId);
            if (dbl.Count == 0) return null;
            ProcessInstance pi = (ProcessInstance)SerializationUtil.Deserialize(dbl[0].InstanceData);
            pi.PersistedVersion = dbl[0].RecordVersion;
            //IList<Token> tokens = GetProcessActiveTokens(instanceId, ds);
            //pi.InitTokenInformation(tokens);
            return pi;
        }

        private void PersistToken(Token tok, SoodaSession ss)
        {
            TokenDbList dbl = TokenDb.GetList(TokenDbField.Id == tok.TokenId);
            TokenDb tdb = null;
            if (dbl.Count > 0)
            {
                tdb = dbl[0];
            }
            else
            {
                tdb = new TokenDb();
                tdb.Id = tok.TokenId;
            }
            UpdateTokenDb(tok, tdb);
        }

        public void UpdateProcessInstance(ProcessInstance pi, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            ProcessInstanceDb pdb = ProcessInstanceDb.Load(ss.Transaction, pi.InstanceId);
            pi.Passivate();
            IList<Token> toks = pi.GetAllTokens();
            foreach (Token tok in toks)
            {
                if (tok.Dirty)
                {
                    PersistToken(tok, ss);
                }
            }
            pdb.InstanceData = SerializationUtil.Serialize(pi);
            pdb.RecordVersion = pdb.RecordVersion + 1;
        }

        public ProcessInstance InitializeNewProcessInstance(string definitionId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            ProcessInstanceDb pdb = new ProcessInstanceDb(ss.Transaction);
            pdb.DefinitionId = definitionId;
            pdb.RecordVersion = 0;
            pdb.Status = ProcessStatus.Alive;
            pdb.InstanceId = Guid.NewGuid().ToString("N");
            ProcessInstance pi = new ProcessInstance();
            pi.InstanceId = pdb.InstanceId;
            pi.ProcessDefinitionId = definitionId;
            pi.InitTokenInformation(new List<Token>());
            return pi;
        }

       

        public void UpdateToken(Token tok, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            TokenDb tdb = TokenDb.Load(ss.Transaction, tok.TokenId);
            tdb.PlaceId = tok.PlaceId;
            tdb.Status = (int)tok.Status;
            tdb.Mode = (int)tok.Mode;
            tdb.ProcessInstance = tok.ProcessInstanceId;
            tdb.RecordVersion = tdb.RecordVersion + 1;
        }

        public Token GetToken(string tokenId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            TokenDbList dbl = TokenDb.GetList(ss.Transaction, TokenDbField.Id == tokenId);
            if (dbl.Count == 0) throw new Exception("Token not found");
            return ToToken(dbl[0]);
        }

        private Token ToToken(TokenDb tdb)
        {
            Token tok = new Token();
            tok.Mode = (TokenMode) tdb.Mode;
            tok.TokenId = tdb.Id;
            tok.ProcessInstanceId = tdb.ProcessInstance;
            tok.Status = (NGinn.Engine.TokenStatus)tdb.Status;
            tok.PlaceId = tdb.PlaceId;
            tok.Dirty = false;
            tok.PersistedVersion = tdb.RecordVersion;
            return tok;
        }

        private void UpdateTokenDb(Token tok, TokenDb tdb)
        {
            tdb.Mode = (int)tok.Mode;
            tdb.ProcessInstance = tok.ProcessInstanceId;
            tdb.PlaceId = tok.PlaceId;
            tdb.Status = (int)tok.Status;
            //if (tok.PersistedVersion != tdb.RecordVersion) throw new Exception(string.Format("Record version mismatch when persisting token {0}", tok.TokenId));
            tdb.RecordVersion = tdb.RecordVersion + 1;
        }

        public IList<Token> GetProcessActiveTokens(string instanceId, NGinn.Engine.Services.Dao.INGDataSession ds)
        {
            SoodaSession ss = (SoodaSession)ds;
            TokenDbList dbl = TokenDb.GetList(ss.Transaction, TokenDbField.ProcessInstance == instanceId && TokenDbField.Status.In((int)TokenStatus.READY, (int)TokenStatus.WAITING, (int)TokenStatus.WAITING_ENABLED, (int)TokenStatus.WAITING_ALLOCATED, (int) TokenStatus.CONSUMED, (int) TokenStatus.CANCELLED));
            List<Token> lt = new List<Token>();
            foreach(TokenDb tdb in dbl)
            {
                lt.Add(ToToken(tdb));
            }
            return lt;
        }

        #endregion

        #region IProcessInstanceRepository Members


        public IList<string> SelectProcessesWithReadyTokens()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            using (SoodaTransaction st = new SoodaTransaction())
            {
                TokenDbList dbl = TokenDb.GetList(TokenDbField.Status == (int)TokenStatus.READY, 100);
                foreach (TokenDb tdb in dbl)
                {
                    if (!dict.ContainsKey(tdb.ProcessInstance)) dict[tdb.ProcessInstance] = tdb.ProcessInstance;
                }
            }
            return new List<string>(dict.Keys);
        }

        #endregion

        #region IProcessInstanceRepository Members


        public string GetProcessOutputXml(string instanceId)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}

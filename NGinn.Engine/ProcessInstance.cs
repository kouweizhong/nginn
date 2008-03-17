using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NLog;

namespace NGinn.Engine
{
    [Serializable]
    public class ProcessInstance
    {
        private string _instId;
        
        public ProcessDefinition Process
        {
            get { return null; }
        }

        public string InstanceId
        {
            get { return _instId; }
        }

        /// <summary>
        /// true if process has not finished yet (has tokens that did not reach end place)
        /// </summary>
        public bool IsAlive
        {
            get { return false; }
        }

        /// <summary>
        /// returns all tokens in the process instance
        /// </summary>
        /// <returns></returns>
        public IList<Token> GetTokens()
        {
            return null;
        }

        /// <summary>
        /// executes one or more process steps
        /// returns true - if process could continue
        /// returns false - if process cannot continue
        /// </summary>
        /// <returns></returns>
        public bool Kick()
        {
            //1. begin transaction
            //2. find kickable token. if not found, return false
            //3. kick that token
            //4. commit transaction
            return true;
        }

        
    }
}

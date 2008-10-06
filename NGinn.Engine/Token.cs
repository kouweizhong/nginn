using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;
using System.Collections;
using NGinn.Lib.Interfaces;

namespace NGinn.Engine
{
    /// <summary>
    /// Tryb tokena 
    /// </summary>
    public enum TokenMode
    {
        LIVE = 1, ///¿ywy token reprezentuj¹cy normalny przebieg procesu
        DEAD = 2  ///martwy token, u¿ywany do œledzenia przebiegów nieaktywnych w OR-split lub po anulowaniu
    }

    /// <summary>
    /// Possible statuses of a token
    /// </summary>
    public enum TokenStatus
    {
        READY = 1, ///token in place, ready for further processing (it is possible that it will enable some transitions)
        WAITING = 2, ///token is waiting because it cannot enable any transition yet (transition can't fire because of other conditions)
        LOCKED_ENABLED = 3, ///token has been pre-selected for an enabled transition, so the token belongs to at least one active transition and waits for the transition to be selected or completed
        LOCKED_ALLOCATED = 4, ///token has been assigned to a transition (in case of deferred choice, this is after the choice has been made)
        CANCELLED = 5, ///token has been cancelled, all transitions with this token also have been cancelled
        CONSUMED = 6,    ///token has been consumed by a transition, it no longer exists
    }

    /// <summary>
    /// Token 
    /// </summary>
    [Serializable]
    public class Token : INGinnPersistent
    {

        private TokenStatus _status;
        private TokenMode _mode;
        private string _placeId;
        private string _id;

        public string TokenId
        {
            get { return _id; }
            set { _id = value; }
        }
        
        public Token()
        {
        }
        
        public TokenStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public string PlaceId
        {
            get { return _placeId; }
            set { _placeId = value; }
        }
        
        
        private List<string> _activeTransitions = new List<string>();

        /// <summary>
        /// List of transition (correlation ids) that have been initiated by the token. If one of these transitions completes
        /// it will consume the token and cancel all the other transitions
        /// </summary>
        public IList<string> ActiveTransitions
        {
            get { return _activeTransitions; }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}). ST: {2}", TokenId, PlaceId, Status);
        }

        public DataObject SaveState()
        {
            DataObject tdob = new DataObject();
            tdob["Id"] = TokenId;
            tdob["PlaceId"] = PlaceId;
            tdob["Status"] = Status.ToString();
            List<string> ls = new List<string>();
            foreach (string tranid in ActiveTransitions)
            {
                ls.Add(tranid);
            }
            tdob["ActiveTransition"] = ls;
            return tdob;
        }

        public void RestoreState(DataObject dob)
        {
            this.TokenId = (string)dob["Id"];
            this.Status = (TokenStatus)Enum.Parse(typeof(TokenStatus), (string)dob["Status"]);
            this.PlaceId = (string)dob["PlaceId"];
            _activeTransitions = new List<string>();
            object t = dob["ActiveTransition"];
            if (t != null)
            {
                if (t is string)
                {
                    _activeTransitions.Add(t as string);
                }
                else if (t is IList)
                {
                    foreach (string s in t as IList)
                    {
                        _activeTransitions.Add(s);
                    }
                }
                else throw new Exception();
            }
        }
    }
}

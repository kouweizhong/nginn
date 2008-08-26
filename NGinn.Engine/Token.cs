using System;
using System.Collections.Generic;
using System.Text;

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
    /// Token - reprezentuje nasz token wêdruj¹cy przez proces
    /// </summary>
    [Serializable]
    public class Token
    {

        private TokenStatus _status;
        private TokenMode _mode;
        private string _placeId;

        public string TokenId;
        public string ProcessInstanceId;
        
        public Token()
        {
            Dirty = true; 
        }

        public TokenMode Mode
        {
            get { return _mode; }
            set { _mode = value; Dirty = true; }
        }

        public TokenStatus Status
        {
            get { return _status; }
            set { _status = value; Dirty = true; }
        }

        public string PlaceId
        {
            get { return _placeId; }
            set { _placeId = value; Dirty = true; }
        }
        /// <summary>version number of persisted token record</summary>
        public int PersistedVersion;
        /// <summary>true if token data has been modified</summary>
        public bool Dirty;
        /// <summary>
        /// List of transition (correlation ids) that have been initiated by the token. If one of these transitions completes
        /// it will consume the token and cancel all the other transitions
        /// </summary>
        public List<string> ActiveTransitions = new List<string>();

        private Dictionary<string, object> _tokenVariables = new Dictionary<string, object>();

        /// <summary>
        /// Token variables. Token holds all 'out' variables from the immediately preceding task.
        /// </summary>
        public IDictionary<string, object> TokenVariables
        {
            get { return _tokenVariables; }
        }

        public override string ToString()
        {
            return string.Format("{1} ({2}). ST: {3}", ProcessInstanceId, TokenId, PlaceId, Status);
        }
    }
}

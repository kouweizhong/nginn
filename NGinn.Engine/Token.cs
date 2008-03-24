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
        LIVE = 1, ///�ywy token reprezentuj�cy normalny przebieg procesu
        DEAD = 2  ///martwy token, u�ywany do �ledzenia przebieg�w nieaktywnych w OR-split lub po anulowaniu
    }

    /// <summary>
    /// Status tokena
    /// </summary>
    public enum TokenStatus
    {
        READY = 1, ///dopiero wpad� w place, jeszcze nie zosta� zainicjowany task (czyli tu b�dzie jeszcze co� si� dzia�o - proces mo�e si� ruszyc dalej)
        WAITING = 2, ///token czeka na pojawienie sie innych bez ktorych nie mozna uruchomic przejscia
        ERROR = 3, ///b�ad przy pr�bie zainicjowania tasku. ze stanu 'error' mo�na wr�ci� do stanu 'ready' - po interwencji
        CANCELLED = 4, ///token anulowany, task wycofany
        FINISHED = 5,    ///token w stanie ko�cowym
        WAITING_TASK = 6 ///task(albo kilka taskow dla danego miejsca) zosta� zainicjowany, czekamy na jego zakonczenie
    }

    /// <summary>
    /// Token - reprezentuje nasz token w�druj�cy przez proces
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
        /// List of transitions that have been initiated by the token. If one of these transitions completes
        /// it will consume the token and cancel all the other transitions
        /// </summary>
        public IList<ActiveTransition> ActiveTransitions = new List<ActiveTransition>();

        private IDictionary<string, object> _tokenVariables = new Dictionary<string, object>();

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

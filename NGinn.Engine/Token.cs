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
        public string TokenId;
        public string ProcessInstanceId;
        public string PlaceId;
        public TokenMode Mode;
        public TokenStatus Status;
        /// <summary>version number of persisted token record</summary>
        public int PersistedVersion;
        /// <summary>true if token data has been modified</summary>
        public bool Dirty = false;
    }
}

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
    /// Status tokena
    /// </summary>
    public enum TokenStatus
    {
        READY = 1, ///dopiero wpad³ w place, jeszcze nie zosta³ zainicjowany task (czyli tu bêdzie jeszcze coœ siê dzia³o - proces mo¿e siê ruszyc dalej)
        WAITING = 2, ///task zosta³ zainicjowany, czekamy na jego zakoñczenie
        ERROR = 3, ///b³ad przy próbie zainicjowania tasku. ze stanu 'error' mo¿na wróciæ do stanu 'ready' - po interwencji
        CANCELLED = 4, ///token anulowany, task wycofany
        FINISHED = 5,    ///token w stanie koñcowym
    }

    /// <summary>
    /// Token - reprezentuje nasz token wêdruj¹cy przez proces
    /// </summary>
    public class Token
    {
        public string TokenId;
        public string ProcessInstanceId;
        public string PlaceId;
        public TokenMode Mode;
        public TokenStatus Status;
    }
}

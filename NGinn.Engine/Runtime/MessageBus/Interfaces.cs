using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.IO;
using NGinn.Lib.Interfaces.MessageBus;

namespace NGinn.Engine.Runtime.MessageBus
{

    
    /// <summary>
    /// ten sluzy do przetwarzania wiadomosci ktore juz zostaly zdeserializowane
    /// </summary>
    public interface IMessageHandler
    {
        void HandleMessage(object msg, IDictionary headers);
    }


    /// <summary>
    /// Rodzaj komunikat�w w kolejce (spos�b serializacji komunikatu)
    /// </summary>
    public enum MessageContentType
    {
        SerializedBinary,   //obiekt, serializowany binarnie
        SerializedSoap,     //obiekt, serializowany jako soap xml
        Text,               //tekst 
        Binary              //dane binarne, nie podlegaj� interpretacji
    }

    /// <summary>
    /// Interfejs do obslugi komunikatow wejsciowych
    /// Input processor odbiera nadchodz�ce komunikaty i uruchamia ich przetwarzanie
    /// (w trybie synchronicznym) - poprzez przekazanie do RawHandlera lub MessageHandlera.
    /// Jesli co� si� nie powiedzie, podejmowana jest pr�ba ponownej obslugi komunikatu
    /// po ustalonym czasie. Jesli wyczerpie sie limit ponownych prob komunikat trafia do 
    /// kolejki 'fail'.
    /// </summary>
    public interface IInputProcessor
    {
        IMessageHandler   MessageHandler { get; set; }
        IMessageInputPort GetInputPort();
        void Start();
    }

    /// <summary>
    /// Interfejs do zarzadzania dzialaniem input processora
    /// </summary>
    public interface IInputProcessorAdmin
    {
        string Name { get;}
        string Status { get; }
        int RetryQueueSize { get;}
        int FailQueueSize { get; }
        int InputQueueSize { get;}
        /// <summary>
        /// Powoduje przerzucenie wszystkich wiadomosci 'failed' do kolejki wejsciowej
        /// </summary>
        void RetryFailedMessages();
        void Start();
        void Stop();
        MessageContentType MessageType { get;}
        IMessageInputPort GetInputPort();
        /// <summary>
        /// Usuwa wiadomosc z kolejki
        /// </summary>
        bool CancelMessage(string id); 
    }

    

    public interface IMessageInputPort
    {
        string SendMessage(object message);
        string SendMessage(object message, IDictionary headers);
        string Endpoint { get; }
    }

    
    public interface IXmlConfigure
    {
        void Configure(XmlElement el);
    }

    /// <summary>
    /// Wyj�tek, kt�ry pozwala powiedzie� �eby nie pr�bowa� ponownego przetwarzania
    /// wiadomo�ci. Normalnie po ka�dym innym wyj�tku system podejmie pr�b� ponownego
    /// obsluzenia komunikatu (pod warunkiem nie przekroczenia maksymalnej liczby pr�b).
    /// Po otrzymaniu MessageException z RetryProcessing = false komunikat nie b�dzie ponownie
    /// przetwarzany.
    /// </summary>
    public class MessageException : Exception
    {
        private bool _retryProcessing;
        public MessageException(Exception innerException, bool retryProcessing)
            : base("Message processing error", innerException)
        {
            _retryProcessing = retryProcessing;
        }

        public bool RetryProcessing
        {
            get { return _retryProcessing; }
        }
    }
}
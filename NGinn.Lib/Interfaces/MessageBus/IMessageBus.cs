using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces.MessageBus
{
    ///<summary>
    ///Message processing delegate
    ///</summary>
    public delegate object MessageHandler(string topic, string sender, object msg);

    public interface IMessageBus
    {
        object Notify(string sender, string topic, object msg, bool async);
        string Subscribe(Type eventType, string eventTopic, MessageHandler handler);
        void Unsubscribe(string subscriptionId);
    }

    [Serializable]
    public class ScheduledMessage
    {
        /// <summary>
        /// Data/godzina o ktorej 'Body' zostanie wrzucone w messagebus
        /// </summary>
        public DateTime DeliverAt;
        /// <summary>
        /// Opakowany komunikat który bêdziemy dostarczaæ
        /// </summary>
        public object Body;

        public ScheduledMessage() { }
        public ScheduledMessage(object body, DateTime deliverAt)
        {
            Body = body;
            DeliverAt = deliverAt;
        }
    }
}

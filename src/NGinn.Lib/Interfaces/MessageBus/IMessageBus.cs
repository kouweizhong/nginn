using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces.MessageBus
{
    /// <summary>
    /// Message context interface for accessing message-related information
    /// </summary>
    public interface IMessageContext
    {
        /// <summary>Message sender</summary>
        string Sender { get; }
        /// <summary>Message topic</summary>
        string Topic { get; }
        /// <summary>
        /// Get/set the return value
        /// </summary>
        object Retval { get; set; }
        /// <summary>
        /// Set this to 'true' to cancel further processing of the message - other
        /// event subscribers will not be notified
        /// </summary>
        bool CancelFurtherProcessing { get; set; }
        /// <summary>
        /// In case of async messages system will retry processing if an error occurs
        /// in a message handler. If you don't want system to retry, set this property to false.
        /// </summary>
        bool RetryAfterError { get; set; }
    }

    ///<summary>
    ///Message processing delegate
    ///</summary>
    public delegate void MessageHandler(object msg, IMessageContext ctx);

    
    /// <summary>
    /// Message bus interface
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Send a message to local message bus 
        /// </summary>
        /// <param name="sender">message sender. not very useful now...</param>
        /// <param name="topic">message topic. Any string, subscribers can filter messages by topic</param>
        /// <param name="msg">message to be sent. Should be serializable if sent asynchronously.</param>
        /// <param name="async">false - process message synchronously, true - send message asynchronously</param>
        /// <returns></returns>
        object Notify(string sender, string topic, object msg, bool async);
        /// <summary>
        /// Send async message to message bus
        /// </summary>
        /// <param name="sender">message sender</param>
        /// <param name="topic">msg topic</param>
        /// <param name="msg">the message</param>
        /// <returns>message id</returns>
        string NotifyAsync(string sender, string topic, object msg);

        /// <summary>
        /// Cancel async message if it hasn't been processed yet
        /// </summary>
        /// <param name="id"></param>
        void CancelAsyncMessage(string id);
        /// <summary>
        /// Subscribe for specified message type and topic
        /// </summary>
        /// <param name="eventType">Type of message that will be received</param>
        /// <param name="eventTopic">Topic. Wildcard ('*') can be used at the end, meaining that all topics starting with the same string will be matched. 
        /// Specify '*' if all topics are to be matched.</param>
        /// <param name="handler">Message handler that will be invoked for matching message</param>
        /// <returns>subscription ID</returns>
        string Subscribe(Type eventType, string eventTopic, MessageHandler handler);
        
        void Unsubscribe(string subscriptionId);
        /// <summary>
        /// Subscribe object - that is, subscribe all instance methods
        /// that are marked with MessageBusSubscriber attribute
        /// </summary>
        /// <param name="obj"></param>
        void SubscribeObject(object obj);
        void UnsubscribeObject(object obj);
        /// <summary>
        /// Subscribe type - subscribe all static methods that are marked
        /// with MessageBusSubscriber attribute
        /// </summary>
        /// <param name="t"></param>
        void SubscribeType(Type t);
    }

    [Serializable]
    public class ScheduledMessage
    {
        /// <summary>
        /// Data/godzina o ktorej 'Body' zostanie wrzucone w messagebus
        /// </summary>
        public DateTime DeliverAt;
        /// <summary>
        /// Opakowany komunikat kt�ry b�dziemy dostarcza�
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

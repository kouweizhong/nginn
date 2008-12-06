using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NGinn.Lib.Interfaces.MessageBus
{
    /// <summary>
    /// Message bus subscriber attribute that can be applied
    /// to static methods
    /// static object MessageHandler(string topic, string sender, object msg);
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MessageBusSubscriberAttribute : Attribute
    {
        public Type EventType;
        public string EventTopic;
        public MessageBusSubscriberAttribute()
        {
        }

        public MessageBusSubscriberAttribute(Type eventType, string eventTopic)
        {
            EventType = eventType;
            EventTopic = eventTopic;
        }
    }
}

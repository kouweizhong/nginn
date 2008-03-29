using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NGinn.MessageBus
{
    /// <summary>
    /// Atrybut który mówi ¿e metoda ma byc powiadamiana przez MessageBus o 
    /// eventach. Sygnatura metody:
    /// static object Metoda(string topic, string sender, object message);
    /// Metoda musi byc statyczna - nie obslugujemy deklaracji dla metod
    /// instancyjnych
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MessageBusListener : Attribute
    {
        public Type EventType;
        public string EventTopic;
        public MessageBusListener()
        {
        }

        public MessageBusListener(Type eventType, string eventTopic)
        {
            EventType = eventType;
            EventTopic = eventTopic;
        }
    }
}

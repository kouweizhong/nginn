using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Xml;

namespace NGinn.Engine.Dao
{
    internal class SerializationUtil
    {
        public static byte[] Serialize(object obj)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
            bf.Serialize(ms, obj);
            return ms.GetBuffer();
        }

        public static object Deserialize(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            BinaryFormatter bf = new BinaryFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
            return bf.Deserialize(ms);
        }
    }
}

using Netcode.Runtime.Communication.Common.Messaging;
using System;

namespace Netcode.Runtime.Communication.Common.Serialization
{
    internal class MessagePackMessageSerializer : IMessageSerializer
    {
        public NetworkMessage Deserialize(byte[] data, Type messageType)
        {
            throw new NotImplementedException();
        }

        public Type GetMessageType(short messageTypeId)
        {
            throw new NotImplementedException();
        }

        public short GetMessageTypeId(Type messageType)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize(NetworkMessage message)
        {
            throw new NotImplementedException();
        }
    }
}

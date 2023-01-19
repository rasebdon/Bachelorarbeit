using Netcode.Runtime.Communication.Common.Messaging;
using System;

namespace Netcode.Runtime.Communication.Common.Serialization
{
    public interface IMessageSerializer
    {
        public NetworkMessage Deserialize(byte[] data, Type messageType);
        public byte[] Serialize(NetworkMessage message);
        
        Type GetMessageType(short messageTypeId);
        short GetMessageTypeId(Type messageType);
    }
}

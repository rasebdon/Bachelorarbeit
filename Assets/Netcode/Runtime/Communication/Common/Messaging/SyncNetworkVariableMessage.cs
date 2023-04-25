using MessagePack;
using Netcode.Channeling;
using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class SyncNetworkVariableMessage : NetworkMessage
    {
        public SyncNetworkVariableMessage(byte[] value, uint variableHash, Guid networkIdentity, ChannelType channelType)
        {
            Value = value;
            VariableHash = variableHash;
            NetworkIdentity = networkIdentity;
            ChannelType = channelType;
        }

        public byte[] Value { get; set; }
        public uint VariableHash { get; set; }
        public Guid NetworkIdentity { get; set; }
        public ChannelType ChannelType { get; set; }
    }
}

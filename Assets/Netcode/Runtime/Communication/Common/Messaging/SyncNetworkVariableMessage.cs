using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class SyncNetworkVariableMessage : NetworkMessage
    {
        public byte[] Value { get; set; }
        public uint VariableHash { get; set; }
        public Guid NetworkIdentity { get; set; }

        public SyncNetworkVariableMessage(byte[] value, uint variableHash, Guid networkIdentity)
        {
            Value = value;
            VariableHash = variableHash;
            NetworkIdentity = networkIdentity;
        }
    }
}

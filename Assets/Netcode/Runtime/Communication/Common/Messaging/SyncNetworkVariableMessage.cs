using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class SyncNetworkVariableMessage : NetworkMessage
    {
        public byte[] Value { get; set; }
        public string VariablePath { get; set; }
        public Guid NetworkIdentity { get; set; }

        public SyncNetworkVariableMessage(byte[] value, string variablePath, Guid networkIdentity)
        {
            Value = value;
            VariablePath = variablePath;
            NetworkIdentity = networkIdentity;
        }
    }
}
